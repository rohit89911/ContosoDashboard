namespace ContosoDashboard.Services;

/// <summary>
/// Storage abstraction for uploaded document files (TC-004). The training implementation
/// (<see cref="LocalFileStorageService"/>) stores files on the local filesystem, outside
/// wwwroot (TC-001). A future AzureBlobStorageService can implement this same interface and be
/// swapped in via dependency injection configuration, without changing DocumentService,
/// controllers, or the database schema (Constitution Principle II; StakeholderDocs
/// §"Azure Migration Design"). No cloud implementation is built now.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves the given stream's content at <paramref name="relativePath"/> under the configured
    /// storage root. The caller is responsible for generating <paramref name="relativePath"/>
    /// (per TC-002/TC-003: the path is generated BEFORE this call, not by this method).
    /// </summary>
    Task UploadAsync(Stream fileStream, string relativePath, string contentType);

    Task DeleteAsync(string relativePath);

    Task<Stream> DownloadAsync(string relativePath);

    /// <summary>
    /// Returns a locator for the stored file. For the local implementation this is simply the
    /// relative path itself — files outside wwwroot are never exposed as a directly navigable
    /// URL; authorized access goes through DocumentsController instead (TC-001, "Security
    /// Considerations"). A future cloud implementation would return a real signed/expiring URL
    /// here.
    /// </summary>
    Task<string> GetUrlAsync(string relativePath, TimeSpan expiration);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredRoot = configuration["DocumentStorage:RootPath"] ?? "AppData/uploads";

        // Resolve relative to the content root (ContosoDashboard/), NOT wwwroot, per TC-001.
        _rootPath = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(environment.ContentRootPath, configuredRoot);
    }

    public async Task UploadAsync(Stream fileStream, string relativePath, string contentType)
    {
        var fullPath = ResolveFullPath(relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileOutputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        fileStream.Position = 0;
        await fileStream.CopyToAsync(fileOutputStream);
    }

    public Task DeleteAsync(string relativePath)
    {
        var fullPath = ResolveFullPath(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string relativePath)
    {
        var fullPath = ResolveFullPath(relativePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task<string> GetUrlAsync(string relativePath, TimeSpan expiration)
    {
        // No direct file URL is exposed for local storage (see interface doc comment above).
        return Task.FromResult(relativePath);
    }

    private string ResolveFullPath(string relativePath)
    {
        // Never trust relativePath to be free of traversal segments even though it is always
        // generated server-side (GUID-based, per TC-002) rather than taken from user input.
        var combined = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootFullPath = Path.GetFullPath(_rootPath);

        if (!combined.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved file path escapes the configured storage root.");
        }

        return combined;
    }
}
