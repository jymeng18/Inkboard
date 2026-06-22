
using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories
{
    public interface IBlockListRepository
    {
    Task<bool> IsBlockedAsync(Guid userId, Guid blockedUserId);
    Task BlockUserAsync(BlockList block);
    Task UnblockUserAsync(BlockList block);
    Task<BlockList?> GetBlockAsync(Guid userId, Guid blockedUserId);
    }
}