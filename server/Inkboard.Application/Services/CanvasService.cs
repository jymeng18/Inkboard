using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services;

public class CanvasService : ICanvasService
{
    private readonly ICanvasRepository _canvasRepository;
    private readonly IPartyRepository _partyRepository;

    public CanvasService(ICanvasRepository canvasRepository, IPartyRepository partyRepository)
    {
        _canvasRepository = canvasRepository;
        _partyRepository = partyRepository;
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
            LastModifiedAt = DateTime.UtcNow,
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
        canvas.LastModifiedAt = DateTime.UtcNow;
        await _canvasRepository.UpdateCanvasAsync(canvas);

        return Result.Ok();
    }
}
