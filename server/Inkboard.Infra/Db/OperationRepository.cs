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
        _context.CanvasOperations.AddRange(operations);
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
