using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services;

public class CanvasService : ICanvasService
{
    private readonly ICanvasRepository _canvasRepository;

    public CanvasService(ICanvasRepository canvasRepository)
    {
        _canvasRepository = canvasRepository;
    }

    public async Task<Result<Canvas>> CreateCanvasAsync(Guid userId, string canvasName)
    {
        Canvas canvas = new()
        {
            OwnerId = userId,
            Name = canvasName,
            SnapshotURL = null, // TODO: Setup Azure blob storage service, keep as empty for now
            LastModifiedAt = DateTime.UtcNow,
        };

        await _canvasRepository.CreateCanvasAsync(canvas);
        return new Result<Canvas> { Data = canvas, IsSuccess = true };
    }

    public async Task<Result> DeleteCanvasAsync(Guid canvasId, Guid userId)
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return new Result { Error = "Canvas not found.", IsSuccess = false };
        }

        // Verify user owns Canvas before deleting
        if (canvas.OwnerId != userId)
        {
            return new Result
            {
                Error = "Unauthorized deletion on this canvas.",
                IsSuccess = false,
            };
        }

        await _canvasRepository.DeleteCanvasAsync(canvas);
        return new Result { IsSuccess = true };
    }

    public async Task<Result<List<Canvas>>> GetAllCanvasesAsync(Guid userId)
    {
        var canvases = await _canvasRepository.GetCanvasesByUserIdAsync(userId);

        // * This is always a success, an empty list is still a success, can never be null
        return new Result<List<Canvas>> { Data = canvases, IsSuccess = true };
    }

    public async Task<Result> RenameCanvas(string newCanvasName, Guid canvasId)
    {
        var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
        if (canvas is null)
        {
            return new Result { Error = "Canvas not found.", IsSuccess = false };
        }
        canvas.Name = newCanvasName;
        canvas.LastModifiedAt = DateTime.UtcNow;
        await _canvasRepository.UpdateCanvasAsync(canvas);

        return new Result { IsSuccess = true };
    }
}
