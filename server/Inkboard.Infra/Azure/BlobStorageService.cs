using System.ComponentModel;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Inkboard.Application.Interfaces;

namespace Inkboard.Infra.Azure;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string ContainerName = "inkboard-canvases";

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    private static string SnapshotName(Guid canvasId) => $"canvas/{canvasId}.png";

    private static string PreviewName(Guid canvasId) => $"canvas/{canvasId}-preview.png";

    // Called once on startup
    public async Task CreateBlobContainerAsync()
    {
        // via docs: container names must be lwoercase
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(
            ContainerName
        );
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
    }

    public Task<string> UploadBlobAsync(Guid canvasId, Stream imageData, string contentType) =>
        UploadAsync(SnapshotName(canvasId), imageData, contentType);

    public Task<Stream?> DownloadSnapshotAsync(Guid canvasId) =>
        DownloadAsync(SnapshotName(canvasId));

    public Task<string> UploadPreviewAsync(Guid canvasId, Stream imageData, string contentType) =>
        UploadAsync(PreviewName(canvasId), imageData, contentType);

    public Task<Stream?> DownloadPreviewAsync(Guid canvasId) =>
        DownloadAsync(PreviewName(canvasId));

    private async Task<string> UploadAsync(string filename, Stream imageData, string contentType)
    {
        BlobClient blobClient = _blobServiceClient
            .GetBlobContainerClient(ContainerName)
            .GetBlobClient(filename);

        // overwrite existing blob
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        };

        try
        {
            await blobClient.UploadAsync(imageData, options);
            return blobClient.Uri.ToString();
        }
        catch (RequestFailedException)
        {
            return string.Empty;
        }
    }

    private async Task<Stream?> DownloadAsync(string filename)
    {
        BlobClient blobClient = _blobServiceClient
            .GetBlobContainerClient(ContainerName)
            .GetBlobClient(filename);

        // TODO: Maybe move to a exceptionhandler chain
        try
        {
            Stream imgData = await blobClient.OpenReadAsync();
            return imgData;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
