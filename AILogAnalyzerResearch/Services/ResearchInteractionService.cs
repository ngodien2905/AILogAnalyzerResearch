using AILogAnalyzerResearch.Data;
using Microsoft.EntityFrameworkCore;

namespace AILogAnalyzerResearch.Services;

public sealed class ResearchInteractionService(
    IDbContextFactory<ResearchDbContext> contextFactory,
    LogImportService logImportService)
{
    public async Task<AiInteraction> AddManualAsync(CreateInteractionRequest request, CancellationToken cancellationToken = default)
    {
        var platform = ResearchCatalogService.RequireText(request.Platform, 60, "AI platform");
        var prompt = ResearchCatalogService.RequireText(request.Prompt, 100_000, "Prompt");
        var response = ResearchCatalogService.RequireText(request.Response, 500_000, "AI response");
        var source = ResearchCatalogService.RequireText(request.Source, 32, "Interaction source");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.TaskAssignments.AnyAsync(item => item.Id == request.TaskAssignmentId, cancellationToken))
        {
            throw new ResearchNotFoundException("Task assignment was not found.");
        }

        var interaction = new AiInteraction
        {
            TaskAssignmentId = request.TaskAssignmentId,
            Platform = platform,
            Plan = request.Plan,
            Prompt = prompt,
            Response = response,
            OccurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow,
            Source = source,
            IsValid = true,
            Classification = new AiClassification
            {
                Category = request.PreliminaryCategory,
                ReviewStatus = ReviewStatus.Pending
            }
        };

        db.AiInteractions.Add(interaction);
        await db.SaveChangesAsync(cancellationToken);
        return interaction;
    }

    public async Task<InteractionImportResult> ImportAsync(
        Stream content,
        string fileName,
        string? defaultParticipantCode = null,
        string? defaultTaskCode = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await logImportService.ParseAsync(content, fileName, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assignments = await db.TaskAssignments
            .Include(item => item.Participant)
            .Include(item => item.CodingTask)
            .ToListAsync(cancellationToken);
        var assignmentByCode = assignments.ToDictionary(
            item => AssignmentKey(item.Participant.ParticipantCode, item.CodingTask.TaskCode),
            item => item,
            StringComparer.OrdinalIgnoreCase);

        var existing = (await db.AiInteractions.AsNoTracking()
                .Select(item => new { item.TaskAssignmentId, item.Platform, item.Prompt, item.Response, item.OccurredAt })
                .ToListAsync(cancellationToken))
            .Select(item => ExactDuplicateKey(item.TaskAssignmentId, item.Platform, item.Prompt, item.Response, item.OccurredAt))
            .ToHashSet(StringComparer.Ordinal);
        var existingContent = (await db.AiInteractions.AsNoTracking()
                .Select(item => new { item.TaskAssignmentId, item.Platform, item.Prompt, item.Response })
                .ToListAsync(cancellationToken))
            .Select(item => ContentDuplicateKey(item.TaskAssignmentId, item.Platform, item.Prompt, item.Response))
            .ToHashSet(StringComparer.Ordinal);

        var imported = 0;
        var invalid = 0;
        var duplicates = 0;
        var rejected = 0;
        var issues = new List<ImportIssue>();
        var source = $"{Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant()} Import";

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var rowNumber = index + 1;
            string participantCode;
            string taskCode;
            try
            {
                participantCode = ResearchCatalogService.NormalizeCode(
                    row.ParticipantCode ?? defaultParticipantCode ?? string.Empty, 16, "Participant code");
                taskCode = ResearchCatalogService.NormalizeCode(
                    row.TaskCode ?? defaultTaskCode ?? string.Empty, 24, "Task code");
            }
            catch (ResearchValidationException exception)
            {
                rejected++;
                issues.Add(new ImportIssue(rowNumber, exception.Message));
                continue;
            }

            if (!assignmentByCode.TryGetValue(AssignmentKey(participantCode, taskCode), out var assignment))
            {
                rejected++;
                issues.Add(new ImportIssue(rowNumber, $"No assignment exists for {participantCode} and {taskCode}."));
                continue;
            }

            var exactKey = ExactDuplicateKey(assignment.Id, row.Platform, row.Prompt, row.Response, row.OccurredAt);
            var contentKey = ContentDuplicateKey(assignment.Id, row.Platform, row.Prompt, row.Response);
            var isDuplicate = row.HasExplicitTimestamp ? !existing.Add(exactKey) : !existingContent.Add(contentKey);
            if (isDuplicate)
            {
                duplicates++;
                issues.Add(new ImportIssue(rowNumber, "Duplicate interaction was skipped."));
                continue;
            }
            existing.Add(exactKey);
            existingContent.Add(contentKey);

            var validationMessages = new List<string>();
            if (string.IsNullOrWhiteSpace(row.Prompt)) validationMessages.Add("Prompt is missing.");
            if (string.IsNullOrWhiteSpace(row.Response)) validationMessages.Add("AI response is missing.");
            if (string.IsNullOrWhiteSpace(row.Platform) || row.Platform.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                validationMessages.Add("AI platform is missing.");

            var isValid = validationMessages.Count == 0;
            if (!isValid) invalid++;
            var interaction = new AiInteraction
            {
                TaskAssignmentId = assignment.Id,
                Platform = string.IsNullOrWhiteSpace(row.Platform) ? "Unknown" : row.Platform.Trim()[..Math.Min(row.Platform.Trim().Length, 60)],
                Plan = row.Plan,
                Prompt = row.Prompt.Trim(),
                Response = row.Response.Trim(),
                OccurredAt = row.OccurredAt,
                Source = source,
                IsValid = isValid,
                ValidationNote = validationMessages.Count == 0 ? null : string.Join(' ', validationMessages),
                Classification = new AiClassification
                {
                    Category = row.Category,
                    ReviewStatus = ReviewStatus.Pending
                }
            };
            db.AiInteractions.Add(interaction);
            imported++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new InteractionImportResult(rows.Count, imported, invalid, duplicates, rejected, issues);
    }

    public async Task ReviewAsync(Guid interactionId, ReviewInteractionRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var interaction = await db.AiInteractions
            .Include(item => item.Classification)
            .FirstOrDefaultAsync(item => item.Id == interactionId, cancellationToken)
            ?? throw new ResearchNotFoundException("AI interaction was not found.");

        interaction.IsValid = request.IsValid;
        interaction.ValidationNote = ResearchCatalogService.NormalizeOptional(request.ValidationNote, 400, "Validation note");
        interaction.Classification ??= new AiClassification { AiInteractionId = interaction.Id };
        interaction.Classification.Category = request.Category;
        interaction.Classification.ReviewStatus = request.Status;
        interaction.Classification.ReviewerNote = ResearchCatalogService.NormalizeOptional(request.ReviewerNote, 400, "Reviewer note");
        interaction.Classification.ReviewedAt = request.Status is ReviewStatus.Reviewed or ReviewStatus.Excluded
            ? DateTimeOffset.UtcNow
            : null;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InteractionReviewItem>> GetReviewQueueAsync(
        ReviewStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.AiInteractions.AsNoTracking()
            .Include(item => item.Classification)
            .Include(item => item.TaskAssignment).ThenInclude(item => item.Participant)
            .Include(item => item.TaskAssignment).ThenInclude(item => item.CodingTask)
            .AsQueryable();
        if (status is not null)
        {
            query = query.Where(item => item.Classification != null && item.Classification.ReviewStatus == status);
        }

        return await query.OrderByDescending(item => item.OccurredAt)
            .Select(item => new InteractionReviewItem(
                item.Id,
                item.TaskAssignment.Participant.ParticipantCode,
                item.TaskAssignment.CodingTask.TaskCode,
                item.Platform,
                item.Prompt,
                item.Response,
                item.OccurredAt,
                item.IsValid,
                item.ValidationNote,
                item.Classification == null ? UsageCategory.Unclassified : item.Classification.Category,
                item.Classification == null ? ReviewStatus.Pending : item.Classification.ReviewStatus,
                item.Classification == null ? null : item.Classification.ReviewerNote))
            .ToListAsync(cancellationToken);
    }

    private static string AssignmentKey(string participantCode, string taskCode) =>
        $"{participantCode.Trim().ToUpperInvariant()}\u001f{taskCode.Trim().ToUpperInvariant()}";

    private static string ExactDuplicateKey(
        Guid assignmentId,
        string platform,
        string prompt,
        string response,
        DateTimeOffset occurredAt) =>
        $"{ContentDuplicateKey(assignmentId, platform, prompt, response)}\u001f{occurredAt.ToUniversalTime():O}";

    private static string ContentDuplicateKey(Guid assignmentId, string platform, string prompt, string response) =>
        $"{assignmentId:N}\u001f{platform.Trim().ToUpperInvariant()}\u001f{prompt.Trim()}\u001f{response.Trim()}";
}
