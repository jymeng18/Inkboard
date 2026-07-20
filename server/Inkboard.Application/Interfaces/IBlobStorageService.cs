namespace Inkboard.Application.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadSnapshotAsync(Guid canvasId, Stream imageData, string contentType);
}