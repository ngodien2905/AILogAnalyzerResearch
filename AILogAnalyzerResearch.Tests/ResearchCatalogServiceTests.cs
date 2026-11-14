using AILogAnalyzerResearch.Data;
using AILogAnalyzerResearch.Services;

namespace AILogAnalyzerResearch.Tests;

public sealed class ResearchCatalogServiceTests
{
    [Fact]
    public async Task Create_and_assign_normalizes_codes_and_is_idempotent()
    {
        var factory = new TestDbContextFactory();
        var service = new ResearchCatalogService(factory);

        var participant = await service.CreateParticipantAsync(new(" p001 ", CourseCode.PRO, " Group A "));
        var task = await service.CreateCodingTaskAsync(new(" t01 ", "Debugging task", null));
        var first = await service.AssignAsync(participant.Id, task.Id);
        var second = await service.AssignAsync(participant.Id, task.Id);

        Assert.Equal("P001", participant.ParticipantCode);
        Assert.Equal("T01", task.TaskCode);
        Assert.Equal("Group A", participant.ResearchGroup);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(await service.GetAssignmentsAsync());
    }

    [Fact]
    public async Task Duplicate_participant_code_is_rejected()
    {
        var factory = new TestDbContextFactory();
        var service = new ResearchCatalogService(factory);
        await service.CreateParticipantAsync(new("P001", CourseCode.PRO, null));

        var exception = await Assert.ThrowsAsync<ResearchValidationException>(
            () => service.CreateParticipantAsync(new(" p001 ", CourseCode.PRF, null)));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task Unsafe_codes_and_outcomes_above_the_scale_are_rejected()
    {
        var factory = new TestDbContextFactory();
        var service = new ResearchCatalogService(factory);
        await Assert.ThrowsAsync<ResearchValidationException>(
            () => service.CreateParticipantAsync(new("../P001", CourseCode.PRO, null)));
        var participant = await service.CreateParticipantAsync(new("P001", CourseCode.PRO, null));
        await Assert.ThrowsAsync<ResearchValidationException>(
            () => service.UpsertOutcomeAsync(participant.Id, "PostTest", 101, 100));
    }
}
