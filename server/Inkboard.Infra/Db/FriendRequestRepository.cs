using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class FriendRequestRepository : IFriendRequestRepository
{
    private readonly AppDbContext _context;

    public FriendRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateFriendReqAsync(FriendRequest friendRequest)
    {
        await _context.FriendRequests.AddAsync(friendRequest);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FriendRequest>> GetAllReqsByUserIdAsync(Guid userId)
    {
        var allReqs = await _context
            .FriendRequests.Where(fr => fr.RequesteeId == userId)
            .Include(fr => fr.Requester) // * we want to know who sent the req
            .ToListAsync();
        return allReqs;
    }

    public Task<FriendRequest?> GetByIdAsync(Guid frId)
    {
        return _context.FriendRequests.FirstOrDefaultAsync(fr => fr.Id == frId);
    }

    // One direction only. Call it twice to catch A and B requesting each other.
    public Task<FriendRequest?> GetPendingBetweenAsync(Guid requesterId, Guid requesteeId)
    {
        return _context.FriendRequests.FirstOrDefaultAsync(fr =>
            fr.RequesterId == requesterId
            && fr.RequesteeId == requesteeId
            && fr.Status == RequestStatus.Pending
        );
    }

    public Task<List<FriendRequest>> GetPendingReqsByUserIdAsync(Guid userId)
    {
        return _context
            .FriendRequests.Where(fr =>
                fr.RequesteeId == userId && fr.Status == RequestStatus.Pending
            )
            .Include(fr => fr.Requester)
            .ToListAsync();
    }

    public async Task RevokeFriendReqAsync(FriendRequest friendRequest)
    {
        friendRequest.Status = RequestStatus.Revoked;
        _context.FriendRequests.Update(friendRequest);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateFriendReqAsync(FriendRequest friendRequest)
    {
        _context.FriendRequests.Update(friendRequest);
        await _context.SaveChangesAsync();
    }
}
