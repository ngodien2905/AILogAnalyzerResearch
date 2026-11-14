using System.Text;
using AILogAnalyzerResearch.Data;
using AILogAnalyzerResearch.Services;

namespace AILogAnalyzerResearch.Tests;

public sealed class ResearchSubmissionAndMetricsTests
{
    [Fact]
    public async Task Submission_validates_type_and_rubric_ranges()
    {
        var (factory, catalog, assignment) = await CreateAssignmentAsync();
        var service = new ResearchSubmissionService(factory, new MemoryFileStorageService());

        await Assert.ThrowsAsync<ResearchValidationException>(() => service.SubmitAsync(
            assignment.Id, Stream("binary"), "malware.exe", "application/octet-stream", true));
        var submission = await service.SubmitAsync(
            assignment.Id, Stream("public class Solution {}"), "solution.cs", "text/plain", true);
        await Assert.ThrowsAsync<ResearchValidationException>(() => service.SaveAssessmentAsync(
            submission.Id, new CreateAssessmentRequest(5, 0, 0, 0, 0)));
        var assessment = await service.SaveAssessmentAsync(
            submission.Id, new CreateAssessmentRequest(4, 2, 1, 1, 2));

        Assert.Equal(10, assessment.TotalScore);
        await using var download = (await service.OpenAsync(submission.Id)).Content;
        Assert.True(download.Length > 0);
    }

    [Fact]
    public async Task Metrics_distinguish_zero_score_from_missing_assessment()
    {
        var factory = new TestDbContextFactory();
        var catalog = new ResearchCatalogService(factory);
        var interactionService = new ResearchInteractionService(factory, new LogImportService());
        var submissionService = new ResearchSubmissionService(factory, new MemoryFileStorageService());
        var participant = await catalog.CreateParticipantAsync(new("P001", CourseCode.PRO, null));
        var task1 = await catalog.CreateCodingTaskAsync(new("T01", "Assessed", null));
        var task2 = await catalog.CreateCodingTaskAsync(new("T02", "Missing assessment", null));
        var assignment1 = await catalog.AssignAsync(participant.Id, task1.Id);
        await catalog.AssignAsync(participant.Id, task2.Id);

        var included = await interactionService.AddManualAsync(new(
            assignment1.Id, "ChatGPT", AiPlan.Free, "Explain", "Answer", PreliminaryCategory: UsageCategory.CodeExplanation));
        await interactionService.ReviewAsync(included.Id, new(UsageCategory.CodeExplanation, ReviewStatus.Reviewed, true));
        await interactionService.AddManualAsync(new(
            assignment1.Id, "ChatGPT", AiPlan.Free, "Pending", "Answer", PreliminaryCategory: UsageCategory.Debugging));
        var invalid = await interactionService.AddManualAsync(new(
            assignment1.Id, "ChatGPT", AiPlan.Free, "Invalid", "Answer", PreliminaryCategory: UsageCategory.Debugging));
        await interactionService.ReviewAsync(invalid.Id, new(UsageCategory.Debugging, ReviewStatus.Reviewed, false));

        var submission = await submissionService.SubmitAsync(
            assignment1.Id, Stream("class Solution {}"), "solution.cs", "text/plain", true);
        await submissionService.SaveAssessmentAsync(submission.Id, new CreateAssessmentRequest(0, 0, 0, 0, 0));
        var summary = await new ResearchMetricsService(factory).GetParticipantSummaryAsync(participant.Id);

        Assert.Equal(1, summary.ReviewedPromptCount);
        Assert.Equal(0, summary.Assignments.Single(item => item.TaskCode == "T01").CodingScore);
        Assert.Null(summary.Assignments.Single(item => item.TaskCode == "T02").CodingScore);
        Assert.Equal(100, Assert.Single(summary.Categories).Percentage);
    }

    private static async Task<(TestDbContextFactory Factory, ResearchCatalogService Catalog, TaskAssignment Assignment)> CreateAssignmentAsync()
    {
        var factory = new TestDbContextFactory();
        var catalog = new ResearchCatalogService(factory);
        var participant = await catalog.CreateParticipantAsync(new("P001", CourseCode.PRO, null));
        var task = await catalog.CreateCodingTaskAsync(new("T01", "Task", null));
        var assignment = await catalog.AssignAsync(participant.Id, task.Id);
        return (factory, catalog, assignment);
    }

    private static MemoryStream Stream(string value) => new(Encoding.UTF8.GetBytes(value));
}
