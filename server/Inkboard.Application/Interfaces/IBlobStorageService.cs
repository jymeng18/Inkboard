#nullable enable
namespace Inkboard.Application.Interfaces;

public interface IBlobStorageService
{
    Task CreateBlobContainerAsync();
    Task<string> UploadBlobAsync(Guid canvasId, Stream imageData, string contentType);
    Task<Stream?> DownloadSnapshotAsync(Guid canvasId);
}