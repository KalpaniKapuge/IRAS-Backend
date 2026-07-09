// IRAS.Application/Common/Storage/IFileStorage.cs
namespace IRAS.Application.Common.Storage
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream content, string relativeFolder, string fileName, CancellationToken ct);
        Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct);
        Task DeleteAsync(string storedPath, CancellationToken ct);
    }
}
