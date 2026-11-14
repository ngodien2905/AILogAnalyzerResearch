using System.ComponentModel.DataAnnotations;

namespace AILogAnalyzerResearch.Data;

public enum CourseCode
{
    PRF,
    PRO,
    PRN,
    SWP
}

public enum AiPlan
{
    Unknown,
    Free,
    Paid
}

public enum UsageCategory
{
    Unclassified,
    ConceptExplanation,
    RequirementAnalysis,
    SolutionPlanning,
    CodeGeneration,
    FullSolutionRequest,
    Debugging,
    CodeExplanation,
    Refactoring,
    Testing,
    Verification
}

public enum ReviewStatus
{
    Pending,
    Reviewed,
    NeedsAttention,
    Excluded
}

public sealed class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(16)]
    public string ParticipantCode { get; set; } = string.Empty;

    public CourseCode Course { get; set; }

    [StringLength(40)]
    public string? ResearchGroup { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<TaskAssignment> Assignments { get; set; } = [];
    public List<ResearchOutcome> Outcomes { get; set; } = [];
}

public sealed class CodingTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(24)]
    public string TaskCode { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<TaskAssignment> Assignments { get; set; } = [];
}

public sealed class TaskAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;
    public Guid CodingTaskId { get; set; }
    public CodingTask CodingTask { get; set; } = null!;
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<AiInteraction> Interactions { get; set; } = [];
    public List<SourceSubmission> Submissions { get; set; } = [];
}

public sealed class AiInteraction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskAssignmentId { get; set; }
    public TaskAssignment TaskAssignment { get; set; } = null!;

    [Required, StringLength(60)]
    public string Platform { get; set; } = string.Empty;

    public AiPlan Plan { get; set; } = AiPlan.Unknown;

    [Required]
    public string Prompt { get; set; } = string.Empty;

    [Required]
    public string Response { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

    [StringLength(32)]
    public string Source { get; set; } = "Manual";

    public bool IsValid { get; set; } = true;

    [StringLength(400)]
    public string? ValidationNote { get; set; }

    public AiClassification? Classification { get; set; }
}

public sealed class AiClassification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AiInteractionId { get; set; }
    public AiInteraction AiInteraction { get; set; } = null!;
    public UsageCategory Category { get; set; } = UsageCategory.Unclassified;
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;
    public DateTimeOffset? ReviewedAt { get; set; }

    [StringLength(400)]
    public string? ReviewerNote { get; set; }
}

public sealed class SourceSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskAssignmentId { get; set; }
    public TaskAssignment TaskAssignment { get; set; } = null!;

    [Required, StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(600)]
    public string StorageReference { get; set; } = string.Empty;

    [StringLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsFinal { get; set; }
    public CodingAssessment? Assessment { get; set; }
}

public sealed class CodingAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceSubmissionId { get; set; }
    public SourceSubmission SourceSubmission { get; set; } = null!;
    public decimal Correctness { get; set; }
    public decimal CodeQuality { get; set; }
    public decimal SolutionEfficiency { get; set; }
    public decimal TestingErrorHandling { get; set; }
    public decimal AbilityToExplain { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal TotalScore => Correctness + CodeQuality + SolutionEfficiency + TestingErrorHandling + AbilityToExplain;
}

public sealed class ResearchOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    [Required, StringLength(60)]
    public string MetricName { get; set; } = string.Empty;

    public decimal Value { get; set; }
    public decimal? ScaleMaximum { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
