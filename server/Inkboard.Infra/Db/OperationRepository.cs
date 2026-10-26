using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class OperationRepository : IOperationRepository
{
    private readonly AppDbContext _context;

    public OperationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IReadOnlyCollection<CanvasOperation> operations)
    {
        var canvasIds = operations.Select(operation => operation.CanvasId).Distinct().ToList();
        var existingCanvasIds = (
            await _context
                .Canvas.Where(canvas => canvasIds.Contains(canvas.Id))
                .Select(canvas => canvas.Id)
                .ToListAsync()
        ).ToHashSet();

        var userIds = operations
            .Where(operation => operation.UserId.HasValue)
            .Select(operation => operation.UserId!.Value)
            .Distinct()
            .ToList();

        var existingUserIds = (
            await _context
                .Users.Where(user => userIds.Contains(user.Id))
                .Select(user => user.Id)
                .ToListAsync()
        ).ToHashSet();

        // An account may be deleted while its final strokes are still queued.
        // Keep contributions to surviving canvases anonymously; discard work for
        // canvases that were deleted, instead of failing the whole write batch.
        foreach (var operation in operations)
        {
            if (!existingCanvasIds.Contains(operation.CanvasId))
                continue;

            if (operation.UserId.HasValue && !existingUserIds.Contains(operation.UserId.Value))
                operation.UserId = null;

            _context.CanvasOperations.Add(operation);
        }
        await _context.SaveChangesAsync();
    }

    public Task<List<CanvasOperation>> GetByCanvasAsync(Guid canvasId)
    {
        return _context
            .CanvasOperations.Where(op => op.CanvasId == canvasId)
            .OrderBy(op => op.Timestamp)
            .AsNoTracking()
            .ToListAsync();
    }
}
