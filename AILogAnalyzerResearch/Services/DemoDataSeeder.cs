using System.Text;
using AILogAnalyzerResearch.Data;
using Microsoft.EntityFrameworkCore;

namespace AILogAnalyzerResearch.Services;

public sealed class DemoDataSeeder(
    IDbContextFactory<ResearchDbContext> contextFactory,
    ResearchCatalogService catalog,
    ResearchInteractionService interactions,
    ResearchSubmissionService submissions,
    IConfiguration configuration)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Database:SeedDemoData")) return;
        await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            if (await db.Participants.AnyAsync(cancellationToken)) return;
        }

        var participant1 = await catalog.CreateParticipantAsync(new("P001", CourseCode.PRO, "Control"), cancellationToken);
        var participant2 = await catalog.CreateParticipantAsync(new("P002", CourseCode.PRF, "AI-assisted"), cancellationToken);
        var participant3 = await catalog.CreateParticipantAsync(new("P003", CourseCode.SWP, "AI-assisted"), cancellationToken);
        var task1 = await catalog.CreateCodingTaskAsync(new("T01", "Debug a null reference", "Find and fix the defect, then explain the cause."), cancellationToken);
        var task2 = await catalog.CreateCodingTaskAsync(new("T02", "Implement a student registry", "Implement the required CRUD operations and tests."), cancellationToken);

        var assignment1 = await catalog.AssignAsync(participant1.Id, task1.Id, cancellationToken);
        var assignment2 = await catalog.AssignAsync(participant2.Id, task1.Id, cancellationToken);
        var assignment3 = await catalog.AssignAsync(participant3.Id, task2.Id, cancellationToken);

        await AddReviewedAsync(assignment1.Id, "ChatGPT", "Why does this throw NullReferenceException?", "The object is null before the member access.", UsageCategory.Debugging, cancellationToken);
        await AddReviewedAsync(assignment1.Id, "ChatGPT", "Explain null-conditional operators in C#.", "The ?. operator stops member access when the receiver is null.", UsageCategory.ConceptExplanation, cancellationToken);
        await AddReviewedAsync(assignment2.Id, "Claude", "Suggest a debugging plan without writing the full solution.", "Reproduce the failure, inspect the stack trace, then check each nullable reference.", UsageCategory.SolutionPlanning, cancellationToken);
        await AddReviewedAsync(assignment3.Id, "ChatGPT", "Generate a repository class for students.", "Here is a repository implementation with create, read, update, and delete methods.", UsageCategory.CodeGeneration, cancellationToken);
        await AddReviewedAsync(assignment3.Id, "ChatGPT", "How should I test duplicate student IDs?", "Add a test that inserts the same ID twice and asserts the expected validation error.", UsageCategory.Testing, cancellationToken);

        await catalog.UpsertOutcomeAsync(participant1.Id, "PreTest", 57, 100, cancellationToken);
        await catalog.UpsertOutcomeAsync(participant1.Id, "PostTest", 69, 100, cancellationToken);
        await catalog.UpsertOutcomeAsync(participant1.Id, "SelfEfficacy", 3.8m, 5, cancellationToken);

        await using var code = new MemoryStream(Encoding.UTF8.GetBytes("public static string SafeLength(string? value) => value?.Length.ToString() ?? \"0\";"));
        var submission = await submissions.SubmitAsync(assignment1.Id, code, "solution.cs", "text/plain", true, cancellationToken);
        await submissions.SaveAssessmentAsync(
            submission.Id,
            new CreateAssessmentRequest(4, 1.5m, 0.8m, 0.7m, 1.5m, "Demo rubric assessment."),
            cancellationToken);
    }

    private async Task AddReviewedAsync(
        Guid assignmentId,
        string platform,
        string prompt,
        string response,
        UsageCategory category,
        CancellationToken cancellationToken)
    {
        var interaction = await interactions.AddManualAsync(
            new CreateInteractionRequest(assignmentId, platform, AiPlan.Free, prompt, response, PreliminaryCategory: category, Source: "Demo Seed"),
            cancellationToken);
        await interactions.ReviewAsync(
            interaction.Id,
            new ReviewInteractionRequest(category, ReviewStatus.Reviewed, true, "Demo review"),
            cancellationToken);
    }
}
