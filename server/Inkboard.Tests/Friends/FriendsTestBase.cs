using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;

namespace Inkboard.Tests.Friends;

public abstract class FriendsTestBase : TestBase
{
    protected FriendsListService Service { get; private set; } = null!;

    [TestInitialize]
    public void InitFriendsService()
    {
        Service = new FriendsListService(
            new FriendRequestRepository(Context),
            new FriendshipRepository(Context),
            new UserRepository(Context)
        );
    }

    /* Seeds a request directly against the context, bypassing SendFriendReqAsync,
     * for setting up states the service itself refuses to create (e.g. a
     * Friendship existing alongside a still-Pending request). */
    protected async Task<FriendRequest> SeedFriendRequestAsync(
        Guid requesterId,
        Guid requesteeId,
        RequestStatus status = RequestStatus.Pending
    )
    {
        var request = new FriendRequest
        {
            RequesterId = requesterId,
            RequesteeId = requesteeId,
            Status = status,
        };
        Context.FriendRequests.Add(request);
        await Context.SaveChangesAsync();
        return request;
    }

    protected async Task<Friendship> SeedFriendshipAsync(Guid userIdAlpha, Guid userIdBeta)
    {
        var friendship = new Friendship { UserId1 = userIdAlpha, UserId2 = userIdBeta };
        await new FriendshipRepository(Context).CreateFriendshipAsync(friendship);
        return friendship;
    }
}
