#nullable enable
using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories;

public interface IFriendRequestRepository
{
    Task<FriendRequest?> GetByIdAsync(Guid requestId);
    Task<FriendRequest?> GetPendingBetweenAsync(Guid requesterId, Guid requesteeId);
    Task<List<FriendRequest>> GetPendingReqsByUserIdAsync(Guid requesteeId);
    Task<List<FriendRequest>> GetAllReqsByUserIdAsync(Guid requesteeId); // ! Might not be needed, keep for now.
    Task CreateFriendReqAsync(FriendRequest friendRequest);
    Task UpdateFriendReqAsync(FriendRequest friendRequest);
    Task RevokeFriendReqAsync(FriendRequest friendRequest);
}
