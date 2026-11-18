using AILogAnalyzerResearch.Data;

namespace AILogAnalyzerResearch.Services;

public sealed class ResearchValidationException(string message) : Exception(message);

public sealed class ResearchNotFoundException(string message) : Exception(message);

public sealed record CreateParticipantRequest(string ParticipantCode, CourseCode Course, string? ResearchGroup);

public sealed record CreateCodingTaskRequest(string TaskCode, string Title, string? Description);

public sealed record CreateInteractionRequest(
    Guid TaskAssignmentId,
    string Platform,
    AiPlan Plan,
    string Prompt,
    string Response,
    DateTimeOffset? OccurredAt = null,
    UsageCategory PreliminaryCategory = UsageCategory.Unclassified,
    string Source = "Manual");

public sealed record ImportIssue(int RowNumber, string Message);

public sealed record InteractionImportResult(
    int TotalRows,
    int ImportedRows,
    int InvalidRows,
    int DuplicateRows,
    int RejectedRows,
    IReadOnlyList<ImportIssue> Issues);

public sealed record ReviewInteractionRequest(
    UsageCategory Category,
    ReviewStatus Status,
    bool IsValid,
    string? ReviewerNote = null,
    string? ValidationNote = null);

public sealed record InteractionReviewItem(
    Guid InteractionId,
    string ParticipantCode,
    string TaskCode,
    string Platform,
    string Prompt,
    string Response,
    DateTimeOffset OccurredAt,
    bool IsValid,
    string? ValidationNote,
    UsageCategory Category,
    ReviewStatus ReviewStatus,
    string? ReviewerNote);

public sealed record CreateAssessmentRequest(
    decimal Correctness,
    decimal CodeQuality,
    decimal SolutionEfficiency,
    decimal TestingErrorHandling,
    decimal AbilityToExplain,
    string? Notes = null);

public sealed record SubmissionDownload(string FileName, string ContentType, Stream Content);

public sealed record ResearchExportFile(string FileName, string ContentType, byte[] Content);

public sealed record CategoryMetric(UsageCategory Category, int Count, decimal Percentage);

public sealed record AssignmentMetrics(
    Guid AssignmentId,
    string TaskCode,
    string TaskTitle,
    int ReviewedPromptCount,
    IReadOnlyList<CategoryMetric> Categories,
    decimal? CodingScore,
    bool HasSubmission,
    bool HasAssessment);

public sealed record ResearchOutcomeValue(string MetricName, decimal Value, decimal? ScaleMaximum);

public sealed record ParticipantResearchSummary(
    Guid ParticipantId,
    string ParticipantCode,
    CourseCode Course,
    string? ResearchGroup,
    int ReviewedPromptCount,
    IReadOnlyList<CategoryMetric> Categories,
    IReadOnlyList<AssignmentMetrics> Assignments,
    IReadOnlyList<ResearchOutcomeValue> Outcomes);

public sealed record DashboardMetrics(
    int ParticipantCount,
    int CodingTaskCount,
    int InteractionCount,
    int PendingReviewCount,
    int InvalidInteractionCount,
    int SubmissionCount,
    int AssessmentCount);
