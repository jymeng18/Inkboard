using System.Buffers.Binary;
using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services;

public class CanvasService : ICanvasService
{
    // Longest edge of a dashboard preview thumbnail, in pixels.
    private const int PreviewMaxDimension = 400;

    private const int MaxOperationDataLength = 256 * 1024;

    private readonly ICanvasRepository _canvasRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IImageValidator _imageValidator;
    private readonly IOperationRepository _operationRepository;

    public CanvasService(
        ICanvasRepository canvasRepository,
        IPartyRepository partyRepository,
        IBlobStorageService blobStorageService,
        IImageValidator imageValidator,
        IOperationRepository operationRepository
    )
    {
        _canvasRepository = canvasRepository;
        _partyRepository = partyRepository;
        _blobStorageService = blobStorageService;
        _imageValidator = imageValidator;
        _operationRepository = operationRepository;
    }

    public async Task<Result<Canvas>> CreateCanvasAsync(Guid userId, string canvasName)
    {
        if (canvasName.Length > 50)
        {
            return Result<Canvas>.Fail(ErrorType.Validation, "Canvas name exceeds 50 characters.");
        }

        Canvas canvas = new()
        {
            OwnerId = userId,
            Name = canvasName,
            SnapshotURL = null,
            LastModifiedAt = DateTimeOffset.UtcNow,
        };

        await _canvasRepository.CreateCanvasAsync(canvas);
        return Result<Canvas>.Ok(data: canvas);
    }

    public async Task<Result> DeleteCanvasAsync(Guid canvasId, Guid userId)
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return Result.Fail(ErrorType.NotFound, "Canvas not found.");
        }

        // Verify user owns Canvas before deleting
        if (canvas.OwnerId != userId)
        {
            return Result.Fail(ErrorType.Forbidden, error: "Unauthorized deletion on this canvas.");
        }

        await _canvasRepository.DeleteCanvasAsync(canvas);
        return Result.Ok();
    }

    public async Task<Result> ForceEndSessionAsync(Guid canvasId, Guid userId)
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
            return Result.Fail(ErrorType.NotFound, "Canvas not found.");

        // When canvas session ends, destroy the link from party to active Canvas
        var party = await _partyRepository.GetActivePartyForUserAsync(userId);
        if (party is null)
            return Result.Fail(ErrorType.NotFound, "Party not found.");

        if (party.CanvasId != canvasId)
            return Result.Fail(ErrorType.Forbidden, "Canvas does not belong to Party.");

        party.CanvasId = null;
        await _partyRepository.UpdatePartyAsync(party);

        return Result.Ok();
    }

    public async Task<Result<List<Canvas>>> GetAllCanvasesAsync(Guid userId)
    {
        var canvases = await _canvasRepository.GetCanvasesByUserIdAsync(userId);

        // * This is always a success, an empty list is still a success, can never be null
        return Result<List<Canvas>>.Ok(data: canvases);
    }

    public async Task<Result> RenameCanvas(string newCanvasName, Guid canvasId, Guid userId)
    {
        if (newCanvasName.Length > 50)
        {
            return Result.Fail(ErrorType.Validation, "Canvas name exceeds 50 characters.");
        }

        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return Result.Fail(ErrorType.NotFound, "Canvas not found.");
        }

        if (userId != canvas.OwnerId)
        {
            return Result.Fail(ErrorType.Forbidden, "Canvas does not belong to you.");
        }

        canvas.Name = newCanvasName;
        canvas.LastModifiedAt = DateTimeOffset.UtcNow;
        await _canvasRepository.UpdateCanvasAsync(canvas);

        return Result.Ok();
    }

    public async Task<Result> UploadSnapshotAsync(
        Guid canvasId,
        Guid ownerId,
        Stream imageData,
        string contentType
    )
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return Result.Fail(ErrorType.NotFound, "Canvas not found.");
        }

        // Saving current canvas snapshot is only ever done so on Owners path,
        // when they ForceEndSession or in between interval saves
        if (canvas.OwnerId != ownerId)
        {
            return Result.Fail(ErrorType.Forbidden, "Canvas does not belong to you.");
        }

        // KonvaJS stage.toBlob({}) mime tpye is image/png
        const string expectedContentType = "image/png";
        if (string.IsNullOrEmpty(contentType) || !contentType.StartsWith(expectedContentType))
        {
            return Result.Fail(ErrorType.Validation, "Invalid data format.");
        }

        var result = await ImageDataValidator(imageData);
        if (!result.IsSuccess)
        {
            return Result.Fail(result.ErrorType, result.Error);
        }

        // cheap checks above proved the header looks like a PNG, this proves
        // the whole payload actually decodes as one.
        if (!await _imageValidator.IsDecodablePngAsync(imageData))
        {
            return Result.Fail(
                ErrorType.Validation,
                "Image payload failed to decode as a valid PNG."
            );
        }

        var uri = await _blobStorageService.UploadBlobAsync(canvasId, imageData, contentType);
        if (string.IsNullOrEmpty(uri))
        {
            return Result.Fail(
                ErrorType.Unexpected,
                "Unexpected error occured uploading snapshot."
            );
        }

        // Render and store the dashboard thumbnail once, here on the write path, so
        // gallery reads never re-download and re-scale the full image. Best effort:
        // the snapshot is the source of truth, and a missing preview falls back
        if (imageData.CanSeek)
        {
            imageData.Position = 0;
            var preview = await _imageValidator.CreatePngThumbnailAsync(
                imageData,
                PreviewMaxDimension
            );
            if (preview is not null)
            {
                await using (preview)
                {
                    await _blobStorageService.UploadPreviewAsync(
                        canvasId,
                        preview,
                        expectedContentType
                    );
                }
            }
        }

        // Update canvas properties
        canvas.LastModifiedAt = DateTimeOffset.UtcNow;
        canvas.SnapshotTakenAt = DateTimeOffset.UtcNow;
        canvas.SnapshotURL = uri;
        await _canvasRepository.UpdateCanvasAsync(canvas);

        return Result.Ok();
    }

    public async Task<Result<Stream>> GetSnapshotAsync(Guid canvasId, Guid requesterId)
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return Result<Stream>.Fail(ErrorType.NotFound, "Canvas not found.");
        }

        if (string.IsNullOrEmpty(canvas.SnapshotURL))
        {
            return Result<Stream>.Fail(ErrorType.NotFound, "Snapshot not found.");
        }

        // * Only owner in the session
        bool isValidMember = false;
        if (canvas.OwnerId != requesterId)
        {
            var activeParty = await _partyRepository.GetActivePartyForUserAsync(requesterId);
            if (activeParty is null)
            {
                return Result<Stream>.Fail(ErrorType.Forbidden, "User is not in a Party.");
            }

            // * Ensure that a user cant grab someone elses snapshot just bc they are in an ActiveParty
            if (activeParty.CanvasId != canvasId)
            {
                return Result<Stream>.Fail(ErrorType.Forbidden, "Canvas does not belong to party.");
            }
            isValidMember = true;
        }

        // ! If your not owner, and your not a valid member in the party
        if (canvas.OwnerId != requesterId && !isValidMember)
        {
            return Result<Stream>.Fail(ErrorType.Forbidden, "Canvas does not belong to you.");
        }

        Stream imgData = await _blobStorageService.DownloadSnapshotAsync(canvasId);
        if (imgData is null)
        {
            return Result<Stream>.Fail(ErrorType.NotFound, "Failed to load snapshot.");
        }

        return Result<Stream>.Ok(imgData);
    }

    public async Task<Result<Stream>> GetSnapshotPreviewAsync(Guid canvasId, Guid requesterId)
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return Result<Stream>.Fail(ErrorType.NotFound, "Canvas not found.");
        }

        if (string.IsNullOrEmpty(canvas.SnapshotURL))
        {
            return Result<Stream>.Fail(ErrorType.NotFound, "Snapshot not found.");
        }

        if (canvas.OwnerId != requesterId)
        {
            return Result<Stream>.Fail(ErrorType.Forbidden, "Canvas does not belong to you.");
        }

        // Fast path: stream the thumbnail rendered once at upload time. No blob
        // re-download of the full image, no per-request decode or resize.
        var preview = await _blobStorageService.DownloadPreviewAsync(canvasId);
        if (preview is not null)
        {
            return Result<Stream>.Ok(preview);
        }

        // Fallback for snapshots stored before previews existed, or a preview whose
        // write failed. Downscale once now; the next snapshot repopulates the blob.
        // ! Usually used for Canvases on initial save
        var snapshot = await _blobStorageService.DownloadSnapshotAsync(canvasId);
        if (snapshot is null)
        {
            return Result<Stream>.Fail(ErrorType.NotFound, "Failed to load snapshot.");
        }

        await using (snapshot)
        {
            var thumbnail = await _imageValidator.CreatePngThumbnailAsync(
                snapshot,
                PreviewMaxDimension
            );
            if (thumbnail is null)
            {
                return Result<Stream>.Fail(
                    ErrorType.Unexpected,
                    "Failed to render snapshot preview."
                );
            }

            return Result<Stream>.Ok(thumbnail);
        }
    }

    public async Task<Result<List<string>>> GetOperationsAsync(Guid canvasId, Guid requesterId)
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return Result<List<string>>.Fail(ErrorType.NotFound, "Canvas not found.");
        }

        if (canvas.OwnerId != requesterId)
        {
            var activeParty = await _partyRepository.GetActivePartyForUserAsync(requesterId);
            if (activeParty is null || activeParty.CanvasId != canvasId)
            {
                return Result<List<string>>.Fail(
                    ErrorType.Forbidden,
                    "Canvas does not belong to you."
                );
            }
        }

        List<CanvasOperation> operations = await _operationRepository.GetByCanvasAsync(canvasId);
        List<string> payloads = operations.ConvertAll(op => op.OperationData);
        return Result<List<string>>.Ok(payloads);
    }

    // TODO: Needs optimization if more drawing is done solo
    /// <summary>
    /// Persists a single committed op. Used when a client can't broadcast it over
    /// the hub (a solo owner, or before the group join), so drawing survives a reopen
    /// </summary>
    public async Task<Result> SaveOperationAsync(
        Guid canvasId,
        Guid requesterId,
        int type,
        string operationData
    )
    {
        if (string.IsNullOrEmpty(operationData) || operationData.Length > MaxOperationDataLength)
            return Result.Fail(ErrorType.Validation, "Operation payload is missing or too large.");

        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
            return Result.Fail(ErrorType.NotFound, "Canvas not found.");

        if (canvas.OwnerId != requesterId)
        {
            var activeParty = await _partyRepository.GetActivePartyForUserAsync(requesterId);
            if (activeParty is null || activeParty.CanvasId != canvasId)
                return Result.Fail(ErrorType.Forbidden, "Canvas does not belong to you.");
        }

        var operation = new CanvasOperation
        {
            Id = Guid.NewGuid(),
            Type = Enum.IsDefined(typeof(ActionType), type) ? (ActionType)type : ActionType.Draw,
            OperationData = operationData,
            CanvasId = canvasId,
            UserId = requesterId,
            Timestamp = DateTimeOffset.UtcNow,
        };

        await _operationRepository.AddRangeAsync(new[] { operation });
        return Result.Ok();
    }

    public static async Task<Result> ImageDataValidator(Stream imageData)
    {
        if (imageData?.CanRead is not true)
        {
            return Result.Fail(ErrorType.Validation, "Image is missing or cannot be read.");
        }

        // Must be between 1kb and 10mb
        if (imageData.Length < 1024 || imageData.Length > 10 * 1024 * 1024)
        {
            return Result.Fail(ErrorType.Validation, "Image is too small or too large.");
        }

        // * Read first 24 bytes of the "image/png" 8 bytes (signature) + 16 bytes (img header)
        byte[] pngHeaderBuf = new byte[24];
        long originalPos = imageData.Position;

        int bytesRead = 0;
        while (bytesRead < 24)
        {
            int read = await imageData.ReadAsync(pngHeaderBuf, offset: bytesRead, 24 - bytesRead);
            if (read == 0)
            {
                break;
            }
            bytesRead += read;
        }

        imageData.Position = originalPos;

        if (bytesRead < 24)
        {
            return Result.Fail(ErrorType.Validation, "Image header is malformed, Invalid png.");
        }

        // * Stream imageData is a copy of the reference to the original, so position needs to be reset
        // * after ReadAsync()
        byte[] pngSignatureBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        // * Verify & Read the first 8 bytes(PNG signature) it is always the same
        bool areEqual = pngHeaderBuf.AsSpan(0, 8).SequenceEqual(pngSignatureBytes);
        if (!areEqual)
        {
            return Result.Fail(ErrorType.Validation, "File payload is not a valid PNG image.");
        }

        // * verify width height of png
        // ! Note; Not all bytes in the Img header are 32bit ints
        uint width = BinaryPrimitives.ReadUInt32BigEndian(pngHeaderBuf.AsSpan(16, 4));
        uint height = BinaryPrimitives.ReadUInt32BigEndian(pngHeaderBuf.AsSpan(20, 4));

        if (width < 50 || height < 50)
        {
            // If this occurs, something is really bad
            return Result.Fail(ErrorType.Validation, "Canvas img dimensions are too small. ");
        }

        if (width > 8192 || height > 8192)
        {
            return Result.Fail(
                ErrorType.Validation,
                "Canvas img resolution exceeds maximum allowed limits."
            );
        }

        return Result.Ok();
    }
}
