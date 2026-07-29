using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly AppDbContext _context;

    public FriendshipRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Friendship?> GetByIdAsync(Guid userIdAlpha, Guid userIdBeta)
    {
        var (first, second) = Canonical(userIdAlpha, userIdBeta);
        return _context.Friendships.FirstOrDefaultAsync(f =>
            f.UserId1 == first && f.UserId2 == second
        );
    }

    public Task<bool> AreFriendsAsync(Guid userIdAlpha, Guid userIdBeta)
    {
        var (first, second) = Canonical(userIdAlpha, userIdBeta);
        return _context.Friendships.AnyAsync(f => f.UserId1 == first && f.UserId2 == second);
    }

    /*
     * each side is a clean seek (the key for UserId1, its own index for UserId2)
     * and pulls only the one User(friends) it needs. UNION ALL much faster then OR 
     * operator.
     */
    public async Task<List<User>> GetFriendsListByIdAsync(Guid userId)
    {
        var friendsOfLowerId = _context
            .Friendships.Where(f => f.UserId1 == userId)
            .Select(f => f.User2);

        var friendsOfHigherId = _context
            .Friendships.Where(f => f.UserId2 == userId)
            .Select(f => f.User1);

        return await friendsOfLowerId.Concat(friendsOfHigherId).ToListAsync();
    }

    public async Task CreateFriendshipAsync(Friendship friendship)
    {
        // the CK_Friendships_UserOrder check constraint.
        var (first, second) = Canonical(friendship.UserId1, friendship.UserId2);
        friendship.UserId1 = first;
        friendship.UserId2 = second;

        await _context.Friendships.AddAsync(friendship);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteFriendshipAsync(Friendship friendship)
    {
        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();
    }

    private static (Guid First, Guid Second) Canonical(Guid alpha, Guid beta) =>
        CompareAsUuid(alpha, beta) <= 0 ? (alpha, beta) : (beta, alpha);


    /// <summary>
    /// Postgres orders uuid by its raw bytes, but Guid.CompareTo reads the first
    /// three fields as signed integers, so the two disagree whenever a high bit
    /// is set. Comparing big-endian bytes keeps
    /// "smaller" here identical to "smaller" in the check constraint.
    /// </summary>
    private static int CompareAsUuid(Guid alpha, Guid beta) =>
        alpha
            .ToByteArray(bigEndian: true)
            .AsSpan()
            .SequenceCompareTo(beta.ToByteArray(bigEndian: true));

}
