using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class BlockListRepository : IBlockListRepository
{
    private readonly AppDbContext _context;

    public BlockListRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsBlockedAsync(Guid userId, Guid blockedUserId)
    {
        return await _context.BlockLists.AnyAsync(bl =>
            bl.UserId == userId && bl.BlockedUserId == blockedUserId
        );
    }

    public async Task<BlockList?> GetBlockAsync(Guid userId, Guid blockedUserId)
    {
        return await _context.BlockLists.FirstOrDefaultAsync(bl =>
            bl.UserId == userId && bl.BlockedUserId == blockedUserId
        );
    }

    public async Task BlockUserAsync(BlockList block)
    {
        await _context.BlockLists.AddAsync(block);
        await _context.SaveChangesAsync();
    }

    public async Task UnblockUserAsync(BlockList block)
    {
        _context.BlockLists.Remove(block);
        await _context.SaveChangesAsync();
    }
}
