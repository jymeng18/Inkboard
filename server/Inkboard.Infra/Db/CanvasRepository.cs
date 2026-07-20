using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class CanvasRepository : ICanvasRepository
{
    private readonly AppDbContext _context;

    public CanvasRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateCanvasAsync(Canvas canvas)
    {
        _context.Canvas.Add(canvas);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCanvasAsync(Canvas canvas)
    {
        _context.Canvas.Remove(canvas);
        await _context.SaveChangesAsync();
    }

    public Task<Canvas?> GetCanvasByIdAsync(Guid canvasId)
    {
        return _context.Canvas.FirstOrDefaultAsync(c => c.Id == canvasId);
    }

    // ! Keep for now, prolly wont need it
    public Task<Canvas?> GetCanvasByPartyIdAsync(Guid partyId)
    {
        throw new NotImplementedException();
    }

    public Task<List<Canvas>> GetCanvasesByUserIdAsync(Guid userId)
    {
        return _context.Canvas.Where(c => c.OwnerId == userId).ToListAsync();
    }

    public async Task UpdateCanvasAsync(Canvas canvas)
    {
        _context.Canvas.Update(canvas);
        await _context.SaveChangesAsync();
    }
}
