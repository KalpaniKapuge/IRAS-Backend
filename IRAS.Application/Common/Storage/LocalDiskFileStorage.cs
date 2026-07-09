// IRAS.Application/Common/Storage/LocalDiskFileStorage.cs
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Common.Storage
{
    public class LocalDiskFileStorage : IFileStorage
    {
        private readonly string _root;

        public LocalDiskFileStorage(IOptions<FileStorageOptions> options)
        {
            _root = options.Value.ResumeRootPath;
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

            return Path.GetRelativePath(_root, fullPath);   // store relative paths in the DB
        }

        public Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_root, storedPath));
            if (!fullPath.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved path escapes the storage root.");

            return Task.FromResult<Stream>(File.OpenRead(fullPath));
        }

        public Task DeleteAsync(string storedPath, CancellationToken ct)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_root, storedPath));
            if (File.Exists(fullPath)) File.Delete(fullPath);
            return Task.CompletedTask;
        }
    }
}
