#nullable enable
using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories;

public interface IFriendshipRepository
{
    Task<List<User>> GetFriendsListByIdAsync(Guid userId);
    Task<Friendship?> GetByIdAsync(Guid userIdAlpha, Guid userIdBeta);
    Task<bool> AreFriendsAsync(Guid userIdAlpha, Guid userIdBeta);
    Task CreateFriendshipAsync(Friendship friendship);
    Task DeleteFriendshipAsync(Friendship friendship);
}
