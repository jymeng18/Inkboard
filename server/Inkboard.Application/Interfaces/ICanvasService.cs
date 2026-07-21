using Inkboard.Application.Common;
using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces;

public interface ICanvasService
{
    Task<Result<Canvas>> CreateCanvasAsync(Guid userId, string canvasName);
    Task<Result> DeleteCanvasAsync(Guid canvasId, Guid userId);
    Task<Result<List<Canvas>>> GetAllCanvasesAsync(Guid userId);
    Task<Result> RenameCanvas(string newCanvasName, Guid canvasId, Guid userId);
    Task<Result> ForceEndSessionAsync(Guid canvasId, Guid userId);
}
