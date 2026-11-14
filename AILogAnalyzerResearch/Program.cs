using AILogAnalyzerResearch.Components;
using AILogAnalyzerResearch.Data;
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
