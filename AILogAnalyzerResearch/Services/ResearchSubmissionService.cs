using AILogAnalyzerResearch.Data;
using Microsoft.EntityFrameworkCore;

namespace AILogAnalyzerResearch.Services;

public sealed class ResearchSubmissionService(
    IDbContextFactory<ResearchDbContext> contextFactory,
    IFileStorageService fileStorage)
{
    public const long MaximumFileSize = 20 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        new([".java", ".cs", ".js", ".py", ".zip"], StringComparer.OrdinalIgnoreCase);

    public async Task<SourceSubmission> SubmitAsync(
        Guid taskAssignmentId,
        Stream content,
        string fileName,
        string? contentType,
        bool isFinal,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName)) throw new ResearchValidationException("A source file is required.");
        if (safeFileName.Length > 255) throw new ResearchValidationException("File name cannot exceed 255 characters.");
        if (!AllowedExtensions.Contains(Path.GetExtension(safeFileName)))
        {
            throw new ResearchValidationException("Supported source formats are .java, .cs, .js, .py, and .zip.");
        }

        await using var bufferedContent = await BufferWithLimitAsync(content, MaximumFileSize, cancellationToken);
        var mediaType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        if (mediaType.Length > 120) throw new ResearchValidationException("Content type cannot exceed 120 characters.");
        if (!System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(mediaType, out _))
            throw new ResearchValidationException("Content type is invalid.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.TaskAssignments.AnyAsync(item => item.Id == taskAssignmentId, cancellationToken))
        {
            throw new ResearchNotFoundException("Task assignment was not found.");
        }

        var stored = await fileStorage.SaveAsync(bufferedContent, safeFileName, mediaType, cancellationToken);
        try
        {
            if (isFinal)
            {
                var previousFinals = await db.SourceSubmissions
                    .Where(item => item.TaskAssignmentId == taskAssignmentId && item.IsFinal)
                    .ToListAsync(cancellationToken);
                foreach (var previous in previousFinals) previous.IsFinal = false;
            }

            var submission = new SourceSubmission
            {
                TaskAssignmentId = taskAssignmentId,
                OriginalFileName = safeFileName,
                StorageReference = stored.Reference,
                ContentType = mediaType,
                SizeBytes = stored.SizeBytes,
                IsFinal = isFinal
            };
            db.SourceSubmissions.Add(submission);
            await db.SaveChangesAsync(cancellationToken);
            return submission;
        }
        catch
        {
            await fileStorage.DeleteAsync(stored.Reference, cancellationToken);
            throw;
        }
    }

    public async Task<CodingAssessment> SaveAssessmentAsync(
        Guid sourceSubmissionId,
        CreateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateScore(request.Correctness, 4, "Correctness");
        ValidateScore(request.CodeQuality, 2, "Code quality");
        ValidateScore(request.SolutionEfficiency, 1, "Solution efficiency");
        ValidateScore(request.TestingErrorHandling, 1, "Testing and error handling");
        ValidateScore(request.AbilityToExplain, 2, "Ability to explain");
        var notes = ResearchCatalogService.NormalizeOptional(request.Notes, 1000, "Assessment notes");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.SourceSubmissions.AnyAsync(item => item.Id == sourceSubmissionId, cancellationToken))
        {
            throw new ResearchNotFoundException("Source submission was not found.");
        }

        var assessment = await db.CodingAssessments
            .FirstOrDefaultAsync(item => item.SourceSubmissionId == sourceSubmissionId, cancellationToken);
        if (assessment is null)
        {
            assessment = new CodingAssessment { SourceSubmissionId = sourceSubmissionId };
            db.CodingAssessments.Add(assessment);
        }

        assessment.Correctness = request.Correctness;
        assessment.CodeQuality = request.CodeQuality;
        assessment.SolutionEfficiency = request.SolutionEfficiency;
        assessment.TestingErrorHandling = request.TestingErrorHandling;
        assessment.AbilityToExplain = request.AbilityToExplain;
        assessment.Notes = notes;
        assessment.AssessedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return assessment;
    }

    public async Task<IReadOnlyList<SourceSubmission>> GetSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SourceSubmissions.AsNoTracking()
            .Include(item => item.Assessment)
            .Include(item => item.TaskAssignment).ThenInclude(item => item.Participant)
            .Include(item => item.TaskAssignment).ThenInclude(item => item.CodingTask)
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SubmissionDownload> OpenAsync(Guid sourceSubmissionId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var submission = await db.SourceSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == sourceSubmissionId, cancellationToken)
            ?? throw new ResearchNotFoundException("Source submission was not found.");
        var content = await fileStorage.OpenReadAsync(submission.StorageReference, cancellationToken)
            ?? throw new ResearchNotFoundException("The stored source file was not found.");
        return new SubmissionDownload(submission.OriginalFileName, submission.ContentType, content);
    }

    private static void ValidateScore(decimal value, decimal maximum, string fieldName)
    {
        if (value < 0 || value > maximum)
            throw new ResearchValidationException($"{fieldName} must be between 0 and {maximum}.");
    }

    private static async Task<MemoryStream> BufferWithLimitAsync(Stream source, long maximumBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        var destination = new MemoryStream();
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (destination.Length + read > maximumBytes)
            {
                await destination.DisposeAsync();
                throw new ResearchValidationException($"Source file cannot exceed {maximumBytes / 1024 / 1024} MB.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        destination.Position = 0;
        return destination;
    }
}
