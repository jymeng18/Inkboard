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

    public Task CreateCanvasAsync(Canvas canvas)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCanvasAsync(Canvas canvas)
    {
        throw new NotImplementedException();
    }

    public Task<Canvas?> GetCanvasByIdAsync(Guid canvasId)
    {
        throw new NotImplementedException();
    }

    // ! Keep for now, prolly wont need it
    public Task<Canvas?> GetCanvasByPartyIdAsync(Guid partyId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Canvas>> GetCanvasesByUserIdAsync(Guid userId)
    {
        var userCanvases = await _context.Canvas.Where(c => c.OwnerId == userId).ToListAsync();
        return userCanvases;
    }

    public Task UpdateCanvasAsync(Canvas canvas)
    {
        throw new NotImplementedException();
    }
}
