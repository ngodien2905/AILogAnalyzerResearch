using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text;

namespace AILogAnalyzerResearch.Data;

public sealed class ResearchDbContext(DbContextOptions<ResearchDbContext> options) : DbContext(options)
{
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<CodingTask> CodingTasks => Set<CodingTask>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();
    public DbSet<AiClassification> AiClassifications => Set<AiClassification>();
    public DbSet<SourceSubmission> SourceSubmissions => Set<SourceSubmission>();
    public DbSet<CodingAssessment> CodingAssessments => Set<CodingAssessment>();
    public DbSet<ResearchOutcome> ResearchOutcomes => Set<ResearchOutcome>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasIndex(x => x.ParticipantCode).IsUnique();
            entity.Property(x => x.Course).HasConversion<string>().HasMaxLength(8);
            entity.ToTable(table => table.HasCheckConstraint("ck_participant_code_not_blank", "participant_code <> ''"));
        });

        modelBuilder.Entity<CodingTask>(entity =>
        {
            entity.HasIndex(x => x.TaskCode).IsUnique();
        });

        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.ParticipantId, x.CodingTaskId }).IsUnique();
            entity.HasOne(x => x.Participant).WithMany(x => x.Assignments).HasForeignKey(x => x.ParticipantId);
            entity.HasOne(x => x.CodingTask).WithMany(x => x.Assignments).HasForeignKey(x => x.CodingTaskId);
        });

        modelBuilder.Entity<AiInteraction>(entity =>
        {
            entity.Property(x => x.Plan).HasConversion<string>().HasMaxLength(16);
            entity.HasOne(x => x.TaskAssignment).WithMany(x => x.Interactions).HasForeignKey(x => x.TaskAssignmentId);
        });

        modelBuilder.Entity<AiClassification>(entity =>
        {
            entity.HasIndex(x => x.AiInteractionId).IsUnique();
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(24);
            entity.HasOne(x => x.AiInteraction).WithOne(x => x.Classification)
                .HasForeignKey<AiClassification>(x => x.AiInteractionId);
        });

        modelBuilder.Entity<SourceSubmission>(entity =>
        {
            entity.HasOne(x => x.TaskAssignment).WithMany(x => x.Submissions).HasForeignKey(x => x.TaskAssignmentId);
        });

        modelBuilder.Entity<CodingAssessment>(entity =>
        {
            entity.HasIndex(x => x.SourceSubmissionId).IsUnique();
            entity.HasOne(x => x.SourceSubmission).WithOne(x => x.Assessment)
                .HasForeignKey<CodingAssessment>(x => x.SourceSubmissionId);
            entity.Property(x => x.Correctness).HasPrecision(5, 2);
            entity.Property(x => x.CodeQuality).HasPrecision(5, 2);
            entity.Property(x => x.SolutionEfficiency).HasPrecision(5, 2);
            entity.Property(x => x.TestingErrorHandling).HasPrecision(5, 2);
            entity.Property(x => x.AbilityToExplain).HasPrecision(5, 2);
            entity.Ignore(x => x.TotalScore);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_assessment_correctness", "correctness >= 0 AND correctness <= 4");
                table.HasCheckConstraint("ck_assessment_code_quality", "code_quality >= 0 AND code_quality <= 2");
                table.HasCheckConstraint("ck_assessment_solution_efficiency", "solution_efficiency >= 0 AND solution_efficiency <= 1");
                table.HasCheckConstraint("ck_assessment_testing", "testing_error_handling >= 0 AND testing_error_handling <= 1");
                table.HasCheckConstraint("ck_assessment_explanation", "ability_to_explain >= 0 AND ability_to_explain <= 2");
            });
        });

        modelBuilder.Entity<ResearchOutcome>(entity =>
        {
            entity.HasIndex(x => new { x.ParticipantId, x.MetricName }).IsUnique();
            entity.Property(x => x.Value).HasPrecision(10, 3);
            entity.Property(x => x.ScaleMaximum).HasPrecision(10, 3);
            entity.HasOne(x => x.Participant).WithMany(x => x.Outcomes).HasForeignKey(x => x.ParticipantId);
            entity.ToTable(table => table.HasCheckConstraint("ck_research_outcome_scale", "scale_maximum IS NULL OR scale_maximum > 0"));
        });

        ApplySnakeCaseNames(modelBuilder);
    }

    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name);
            entity.SetTableName(tableName);

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName($"{(key.IsPrimaryKey() ? "pk" : "ak")}_{tableName}_{JoinProperties(key.Properties)}");
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName($"ix_{tableName}_{JoinProperties(index.Properties)}");
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var principalTable = foreignKey.PrincipalEntityType.GetTableName() ?? foreignKey.PrincipalEntityType.ClrType.Name;
                foreignKey.SetConstraintName($"fk_{tableName}_{ToSnakeCase(principalTable)}_{JoinProperties(foreignKey.Properties)}");
            }
        }
    }

    private static string JoinProperties(IEnumerable<IReadOnlyProperty> properties) =>
        string.Join('_', properties.Select(property => ToSnakeCase(property.Name)));

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0 &&
                (char.IsLower(value[index - 1]) || (index + 1 < value.Length && char.IsLower(value[index + 1]))))
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(character));
        }

        return result.ToString();
    }
}
