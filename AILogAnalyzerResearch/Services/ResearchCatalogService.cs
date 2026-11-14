using AILogAnalyzerResearch.Data;
using Microsoft.EntityFrameworkCore;

namespace AILogAnalyzerResearch.Services;

public sealed class ResearchCatalogService(IDbContextFactory<ResearchDbContext> contextFactory)
{
    public async Task<Participant> CreateParticipantAsync(CreateParticipantRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.ParticipantCode, 16, "Participant code");
        var group = NormalizeOptional(request.ResearchGroup, 40, "Research group");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Participants.AnyAsync(item => item.ParticipantCode == code, cancellationToken))
        {
            throw new ResearchValidationException($"Participant code '{code}' already exists.");
        }

        var participant = new Participant
        {
            ParticipantCode = code,
            Course = request.Course,
            ResearchGroup = group
        };
        db.Participants.Add(participant);
        await db.SaveChangesAsync(cancellationToken);
        return participant;
    }

    public async Task<CodingTask> CreateCodingTaskAsync(CreateCodingTaskRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.TaskCode, 24, "Task code");
        var title = RequireText(request.Title, 160, "Task title");
        var description = NormalizeOptional(request.Description, 1200, "Task description");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.CodingTasks.AnyAsync(item => item.TaskCode == code, cancellationToken))
        {
            throw new ResearchValidationException($"Task code '{code}' already exists.");
        }

        var task = new CodingTask { TaskCode = code, Title = title, Description = description };
        db.CodingTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<TaskAssignment> AssignAsync(Guid participantId, Guid codingTaskId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Participants.AnyAsync(item => item.Id == participantId, cancellationToken))
        {
            throw new ResearchNotFoundException("Participant was not found.");
        }

        if (!await db.CodingTasks.AnyAsync(item => item.Id == codingTaskId, cancellationToken))
        {
            throw new ResearchNotFoundException("Coding task was not found.");
        }

        var existing = await db.TaskAssignments
            .FirstOrDefaultAsync(item => item.ParticipantId == participantId && item.CodingTaskId == codingTaskId, cancellationToken);
        if (existing is not null) return existing;

        var assignment = new TaskAssignment { ParticipantId = participantId, CodingTaskId = codingTaskId };
        db.TaskAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<IReadOnlyList<Participant>> GetParticipantsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Participants.AsNoTracking().OrderBy(item => item.ParticipantCode).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CodingTask>> GetCodingTasksAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.CodingTasks.AsNoTracking();
        if (activeOnly) query = query.Where(item => item.IsActive);
        return await query.OrderBy(item => item.TaskCode).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskAssignment>> GetAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TaskAssignments.AsNoTracking()
            .Include(item => item.Participant)
            .Include(item => item.CodingTask)
            .OrderBy(item => item.Participant.ParticipantCode)
            .ThenBy(item => item.CodingTask.TaskCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResearchOutcome> UpsertOutcomeAsync(
        Guid participantId,
        string metricName,
        decimal value,
        decimal? scaleMaximum,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = RequireText(metricName, 60, "Metric name");
        if (scaleMaximum is <= 0) throw new ResearchValidationException("Scale maximum must be greater than zero.");
        if (scaleMaximum is not null && (value < 0 || value > scaleMaximum))
            throw new ResearchValidationException("Outcome value must be between zero and the scale maximum.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Participants.AnyAsync(item => item.Id == participantId, cancellationToken))
        {
            throw new ResearchNotFoundException("Participant was not found.");
        }

        var outcome = await db.ResearchOutcomes
            .FirstOrDefaultAsync(item => item.ParticipantId == participantId && item.MetricName == normalizedName, cancellationToken);
        if (outcome is null)
        {
            outcome = new ResearchOutcome { ParticipantId = participantId, MetricName = normalizedName };
            db.ResearchOutcomes.Add(outcome);
        }

        outcome.Value = value;
        outcome.ScaleMaximum = scaleMaximum;
        outcome.RecordedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return outcome;
    }

    internal static string NormalizeCode(string value, int maxLength, string fieldName)
    {
        var normalized = RequireText(value, maxLength, fieldName).ToUpperInvariant();
        if (normalized.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ResearchValidationException($"{fieldName} can contain only letters, numbers, hyphens, and underscores.");
        return normalized;
    }

    internal static string RequireText(string value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) throw new ResearchValidationException($"{fieldName} is required.");
        if (normalized.Length > maxLength) throw new ResearchValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        return normalized;
    }

    internal static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ResearchValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        return normalized;
    }
}
