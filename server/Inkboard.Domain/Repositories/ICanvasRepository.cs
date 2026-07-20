#nullable enable
using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories;

public interface ICanvasRepository
{
    Task<Canvas?> GetCanvasByIdAsync(Guid canvasId);
    Task<Canvas?> GetCanvasByPartyIdAsync(Guid partyId); // ! Maybe delete
    Task<List<Canvas>> GetCanvasesByUserIdAsync(Guid userId);
    Task CreateCanvasAsync(Canvas canvas);
    Task UpdateCanvasAsync(Canvas canvas);
    Task DeleteCanvasAsync(Canvas canvas);
}
