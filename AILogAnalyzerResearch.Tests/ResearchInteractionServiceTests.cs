using System.Text;
using AILogAnalyzerResearch.Data;
using AILogAnalyzerResearch.Services;

namespace AILogAnalyzerResearch.Tests;

public sealed class ResearchInteractionServiceTests
{
    [Fact]
    public async Task Import_flags_incomplete_rows_rejects_broken_links_and_skips_duplicates()
    {
        var factory = new TestDbContextFactory();
        var catalog = new ResearchCatalogService(factory);
        var service = new ResearchInteractionService(factory, new LogImportService());
        var participant = await catalog.CreateParticipantAsync(new("P001", CourseCode.PRO, null));
        var task = await catalog.CreateCodingTaskAsync(new("T01", "Task", null));
        await catalog.AssignAsync(participant.Id, task.Id);
        var json = """
            [
              {"participantId":"P001","taskId":"T01","platform":"ChatGPT","prompt":"Help","response":"Answer"},
              {"participantId":"P001","taskId":"T01","platform":"Unknown","prompt":"Debug this","response":""},
              {"participantId":"P999","taskId":"T01","platform":"ChatGPT","prompt":"Missing link","response":"Answer"}
            ]
            """;

        var first = await service.ImportAsync(Stream(json), "logs.json");
        var second = await service.ImportAsync(Stream(json), "logs.json");

        Assert.Equal(3, first.TotalRows);
        Assert.Equal(2, first.ImportedRows);
        Assert.Equal(1, first.InvalidRows);
        Assert.Equal(1, first.RejectedRows);
        Assert.Equal(2, second.DuplicateRows);
        Assert.Equal(1, second.RejectedRows);
    }

    [Fact]
    public async Task Manual_interaction_can_be_reviewed()
    {
        var factory = new TestDbContextFactory();
        var catalog = new ResearchCatalogService(factory);
        var service = new ResearchInteractionService(factory, new LogImportService());
        var participant = await catalog.CreateParticipantAsync(new("P001", CourseCode.PRO, null));
        var task = await catalog.CreateCodingTaskAsync(new("T01", "Task", null));
        var assignment = await catalog.AssignAsync(participant.Id, task.Id);
        var interaction = await service.AddManualAsync(new(
            assignment.Id, "ChatGPT", AiPlan.Free, "Why?", "Because.", PreliminaryCategory: UsageCategory.ConceptExplanation));

        await service.ReviewAsync(interaction.Id, new(
            UsageCategory.ConceptExplanation, ReviewStatus.Reviewed, true, "Checked"));
        var reviewed = Assert.Single(await service.GetReviewQueueAsync(ReviewStatus.Reviewed));

        Assert.Equal(interaction.Id, reviewed.InteractionId);
        Assert.Equal("P001", reviewed.ParticipantCode);
        Assert.Equal("Checked", reviewed.ReviewerNote);
    }

    private static MemoryStream Stream(string value) => new(Encoding.UTF8.GetBytes(value));
}
