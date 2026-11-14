using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AILogAnalyzerResearch.Data;

public sealed class ResearchDbContextFactory : IDesignTimeDbContextFactory<ResearchDbContext>
{
    public ResearchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ResearchDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=ai_log_analyzer;Username=postgres;Password=postgres",
                postgres => postgres.MigrationsAssembly(typeof(ResearchDbContext).Assembly.FullName))
            .Options;

        return new ResearchDbContext(options);
    }
}
