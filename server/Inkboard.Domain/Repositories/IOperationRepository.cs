#nullable enable
using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories;

public interface IOperationRepository
{
    Task AddRangeAsync(IReadOnlyCollection<CanvasOperation> operations);

    // Ordered by Timestamp (uses the (CanvasId, Timestamp) index)
    Task<List<CanvasOperation>> GetByCanvasAsync(Guid canvasId);
}
