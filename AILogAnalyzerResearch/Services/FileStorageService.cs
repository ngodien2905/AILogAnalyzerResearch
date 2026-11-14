using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AILogAnalyzerResearch.Services;

public sealed record StoredFile(string Reference, long SizeBytes, string ContentType);

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string reference, CancellationToken cancellationToken = default);
    Task DeleteAsync(string reference, CancellationToken cancellationToken = default);
}

public sealed class LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration) : IFileStorageService
{
    private readonly string _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configuration["Storage:LocalPath"])
        ? Path.Combine(environment.ContentRootPath, "App_Data", "uploads")
        : configuration["Storage:LocalPath"]!);

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_root);
        var extension = Path.GetExtension(Path.GetFileName(fileName));
        var reference = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(_root, reference.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var destination = File.Create(absolutePath);
        await content.CopyToAsync(destination, cancellationToken);
        return new StoredFile(reference, destination.Length, contentType);
    }

    public Task<Stream?> OpenReadAsync(string reference, CancellationToken cancellationToken = default)
    {
        var path = ResolveReference(reference);
        if (path is null || !File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public Task DeleteAsync(string reference, CancellationToken cancellationToken = default)
    {
        var path = ResolveReference(reference);
        if (path is not null && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string? ResolveReference(string reference)
    {
        var rootWithSeparator = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(_root, reference.Replace('/', Path.DirectorySeparatorChar)));
        return path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? path : null;
    }
}

public sealed class SupabaseFileStorageService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IFileStorageService
{
    private readonly string _baseUrl = (configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is required.")).TrimEnd('/');
    private readonly string _serviceRoleKey = configuration["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is required.");
    private readonly string _bucket = configuration["Supabase:StorageBucket"] ?? "source-code";

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(Path.GetFileName(fileName));
        var reference = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        using var request = CreateRequest(HttpMethod.Post, ObjectPath(reference));
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return new StoredFile(reference, content.CanSeek ? content.Length : 0, contentType);
    }

    public async Task<Stream?> OpenReadAsync(string reference, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, ObjectPath(reference));
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var memory = new MemoryStream();
        await response.Content.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    public async Task DeleteAsync(string reference, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"/storage/v1/object/{Uri.EscapeDataString(_bucket)}");
        request.Content = JsonContent.Create(new { prefixes = new[] { reference } });
        using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, _baseUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.TryAddWithoutValidation("apikey", _serviceRoleKey);
        return request;
    }

    private string ObjectPath(string reference) =>
        $"/storage/v1/object/{Uri.EscapeDataString(_bucket)}/{string.Join('/', reference.Split('/').Select(Uri.EscapeDataString))}";
}
