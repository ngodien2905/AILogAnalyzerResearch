using AILogAnalyzerResearch.Data;
using Microsoft.EntityFrameworkCore;

namespace AILogAnalyzerResearch.Services;

public sealed class ResearchMetricsService(IDbContextFactory<ResearchDbContext> contextFactory)
{
    public async Task<DashboardMetrics> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return new DashboardMetrics(
            await db.Participants.CountAsync(cancellationToken),
            await db.CodingTasks.CountAsync(cancellationToken),
            await db.AiInteractions.CountAsync(cancellationToken),
            await db.AiClassifications.CountAsync(item => item.ReviewStatus == ReviewStatus.Pending, cancellationToken),
            await db.AiInteractions.CountAsync(item => !item.IsValid, cancellationToken),
            await db.SourceSubmissions.CountAsync(cancellationToken),
            await db.CodingAssessments.CountAsync(cancellationToken));
    }

    public async Task<ParticipantResearchSummary> GetParticipantSummaryAsync(
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var participant = await db.Participants.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Outcomes)
            .Include(item => item.Assignments).ThenInclude(item => item.CodingTask)
            .Include(item => item.Assignments).ThenInclude(item => item.Interactions).ThenInclude(item => item.Classification)
            .Include(item => item.Assignments).ThenInclude(item => item.Submissions).ThenInclude(item => item.Assessment)
            .FirstOrDefaultAsync(item => item.Id == participantId, cancellationToken)
            ?? throw new ResearchNotFoundException("Participant was not found.");

        var reviewedInteractions = participant.Assignments
            .SelectMany(item => item.Interactions)
            .Where(IsIncludedInMetrics)
            .ToList();
        var assignments = participant.Assignments
            .OrderBy(item => item.CodingTask.TaskCode)
            .Select(item =>
            {
                var interactions = item.Interactions.Where(IsIncludedInMetrics).ToList();
                var assessedSubmission = item.Submissions
                    .Where(submission => submission.Assessment is not null)
                    .OrderByDescending(submission => submission.IsFinal)
                    .ThenByDescending(submission => submission.SubmittedAt)
                    .FirstOrDefault();
                return new AssignmentMetrics(
                    item.Id,
                    item.CodingTask.TaskCode,
                    item.CodingTask.Title,
                    interactions.Count,
                    BuildCategoryMetrics(interactions),
                    assessedSubmission?.Assessment?.TotalScore,
                    item.Submissions.Count > 0,
                    assessedSubmission is not null);
            })
            .ToList();

        return new ParticipantResearchSummary(
            participant.Id,
            participant.ParticipantCode,
            participant.Course,
            participant.ResearchGroup,
            reviewedInteractions.Count,
            BuildCategoryMetrics(reviewedInteractions),
            assignments,
            participant.Outcomes.OrderBy(item => item.MetricName)
                .Select(item => new ResearchOutcomeValue(item.MetricName, item.Value, item.ScaleMaximum))
                .ToList());
    }

    private static bool IsIncludedInMetrics(AiInteraction interaction) =>
        interaction.IsValid &&
        interaction.Classification is { ReviewStatus: ReviewStatus.Reviewed };

    private static IReadOnlyList<CategoryMetric> BuildCategoryMetrics(IReadOnlyCollection<AiInteraction> interactions)
    {
        if (interactions.Count == 0) return [];
        return interactions
            .Where(item => item.Classification is not null)
            .GroupBy(item => item.Classification!.Category)
            .OrderBy(group => group.Key)
            .Select(group => new CategoryMetric(
                group.Key,
                group.Count(),
                Math.Round(group.Count() * 100m / interactions.Count, 2)))
            .ToList();
    }
}
