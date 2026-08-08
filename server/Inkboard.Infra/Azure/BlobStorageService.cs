using System.ComponentModel;
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

    // Called once on startup
    public async Task CreateBlobContainerAsync()
    {
        // via docs: container names must be lwoercase
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(
            ContainerName
        );
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
    }

    /// <summary>
    /// This helper method is intended to be called when the Canvas decides it
    /// wants to create a save/snapshot (eg. Canvas session ends, a new User joins the session
    /// , or maybe in a n-time interval)
    /// </summary>
    public async Task<string> UploadBlobAsync(Guid canvasId, Stream imageData, string contentType)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(
            ContainerName
        );

        // * will automatically insert blob into canvas/ virtual folder
        string filename = $"canvas/{canvasId}.png";

        BlobClient blobClient = containerClient.GetBlobClient(filename);

        // overwrite existing blob 
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        };

        await blobClient.UploadAsync(imageData, options);

        return blobClient.Uri.ToString();
    }
}
