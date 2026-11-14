using AILogAnalyzerResearch.Components;
using AILogAnalyzerResearch.Data;
using AILogAnalyzerResearch.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "data-protection-keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("AI-Coding-Research-Log-Analyzer")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ResearchDb");
builder.Services.AddDbContextFactory<ResearchDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseInMemoryDatabase("ai-log-analyzer-demo");
        return;
    }

    options.UseNpgsql(connectionString, postgres =>
    {
        postgres.MigrationsAssembly(typeof(ResearchDbContext).Assembly.FullName);
        postgres.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<LogImportService>();
builder.Services.AddScoped<ResearchCatalogService>();
builder.Services.AddScoped<ResearchInteractionService>();
builder.Services.AddScoped<ResearchSubmissionService>();
builder.Services.AddScoped<ResearchMetricsService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddSingleton<IFileStorageService>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var useSupabase = !string.IsNullOrWhiteSpace(configuration["Supabase:Url"]) &&
                      !string.IsNullOrWhiteSpace(configuration["Supabase:ServiceRoleKey"]);
    return useSupabase
        ? ActivatorUtilities.CreateInstance<SupabaseFileStorageService>(services)
        : ActivatorUtilities.CreateInstance<LocalFileStorageService>(services);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ResearchDbContext>>();
    await using var db = await database.CreateDbContextAsync();
    if (db.Database.IsRelational())
    {
        if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await db.Database.MigrateAsync();
        }
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/api/submissions/{id:guid}/download", async Task<IResult> (
    Guid id,
    ResearchSubmissionService submissionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var download = await submissionService.OpenAsync(id, cancellationToken);
        return Results.Stream(
            download.Content,
            download.ContentType,
            download.FileName,
            enableRangeProcessing: true);
    }
    catch (ResearchNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
