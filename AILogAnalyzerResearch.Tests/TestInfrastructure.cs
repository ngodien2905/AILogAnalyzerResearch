using AILogAnalyzerResearch.Data;
using AILogAnalyzerResearch.Services;
using Microsoft.EntityFrameworkCore;

namespace AILogAnalyzerResearch.Tests;

internal sealed class TestDbContextFactory : IDbContextFactory<ResearchDbContext>
{
    private readonly DbContextOptions<ResearchDbContext> _options = new DbContextOptionsBuilder<ResearchDbContext>()
        .UseInMemoryDatabase($"test-{Guid.NewGuid():N}")
        .Options;

    public ResearchDbContext CreateDbContext() => new(_options);

    public Task<ResearchDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}

internal sealed class MemoryFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var reference = $"test/{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        _files[reference] = buffer.ToArray();
        return new StoredFile(reference, buffer.Length, contentType);
    }

    public Task<Stream?> OpenReadAsync(string reference, CancellationToken cancellationToken = default) =>
        Task.FromResult(_files.TryGetValue(reference, out var bytes) ? (Stream?)new MemoryStream(bytes, false) : null);

    public Task DeleteAsync(string reference, CancellationToken cancellationToken = default)
    {
        _files.Remove(reference);
        return Task.CompletedTask;
    }
}
