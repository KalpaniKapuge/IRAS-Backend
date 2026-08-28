// IRAS.Application/Common/Storage/LocalDiskFileStorage.cs
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Common.Storage
{
    public class LocalDiskFileStorage : IFileStorage
    {
        private const string UploadsPrefix = "/uploads/";

        private readonly string _root;
        private readonly string _publicBaseUrl;

        public LocalDiskFileStorage(IOptions<FileStorageOptions> options)
        {
            _root = options.Value.ResumeRootPath;
            _publicBaseUrl = options.Value.LocalPublicBaseUrl.TrimEnd('/');
            Directory.CreateDirectory(_root);
        }

        public async Task<string> SaveAsync(Stream content, string relativeFolder, string fileName, CancellationToken ct)
        {
            // Never trust client file names: we generate our own, and we
            // verify the final path stays inside the root (path traversal guard).
            var folder = Path.Combine(_root, relativeFolder);
            Directory.CreateDirectory(folder);

            var fullPath = Path.GetFullPath(Path.Combine(folder, fileName));
            if (!fullPath.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved path escapes the storage root.");

            await using var target = File.Create(fullPath);
            await content.CopyToAsync(target, ct);

            // Return a genuinely fetchable URL (mirrors SupabaseFileStorage's
            // contract) rather than a bare filesystem path — callers use this
            // both to re-open the file later and to render it directly
            // (<img src>, download links, etc.), so it has to work as both.
            var relativePath = Path.GetRelativePath(_root, fullPath).Replace(Path.DirectorySeparatorChar, '/');
            return $"{_publicBaseUrl}{UploadsPrefix}{relativePath}";
        }

        // Accepts either the URL this class returns, or a bare relative path
        // (defensive fallback for rows written before this URL scheme existed).
        private string ToRelativePath(string storedPath)
        {
            var idx = storedPath.IndexOf(UploadsPrefix, StringComparison.OrdinalIgnoreCase);
            var relative = idx >= 0 ? storedPath[(idx + UploadsPrefix.Length)..] : storedPath;
            return relative.Replace('/', Path.DirectorySeparatorChar);
        }

        public Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_root, ToRelativePath(storedPath)));
            if (!fullPath.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved path escapes the storage root.");

            return Task.FromResult<Stream>(File.OpenRead(fullPath));
        }

        public Task DeleteAsync(string storedPath, CancellationToken ct)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_root, ToRelativePath(storedPath)));
            if (File.Exists(fullPath)) File.Delete(fullPath);
            return Task.CompletedTask;
        }
    }
}
