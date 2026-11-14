using AILogAnalyzerResearch.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AILogAnalyzerResearch.Tests;

public sealed class DemoDataSeederTests
{
    [Fact]
    public async Task Seeder_is_enabled_by_configuration_and_idempotent()
    {
        var factory = new TestDbContextFactory();
        var catalog = new ResearchCatalogService(factory);
        var interactionService = new ResearchInteractionService(factory, new LogImportService());
        var submissionService = new ResearchSubmissionService(factory, new MemoryFileStorageService());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:SeedDemoData"] = "true"
        }).Build();
        var seeder = new DemoDataSeeder(factory, catalog, interactionService, submissionService, configuration);

        await seeder.SeedAsync();
        await seeder.SeedAsync();
        await using var db = await factory.CreateDbContextAsync();

        Assert.Equal(3, await db.Participants.CountAsync());
        Assert.Equal(2, await db.CodingTasks.CountAsync());
        Assert.Equal(5, await db.AiInteractions.CountAsync());
        Assert.Single(await db.CodingAssessments.ToListAsync());
    }
}
