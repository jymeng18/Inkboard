using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;

namespace Inkboard.Tests.Friends;

public abstract class FriendsTestBase : TestBase
{
    protected AppDbContext Context { get; private set; } = null!;
    protected FriendsListService Service { get; private set; } = null!;

    [TestInitialize]
    public void BaseInitialize()
    {
        Context = CreateDbContext();
        Service = CreateFriendsListService(Context);
    }

    [TestCleanup]
    public void BaseCleanup()
    {
        Context.Dispose();
    }

    protected static FriendsListService CreateFriendsListService(AppDbContext context)
    {
        return new FriendsListService(
            new FriendRequestRepository(context),
            new FriendshipRepository(context),
            new UserRepository(context)
        );
    }

    protected static async Task<User> SeedUserAsync(AppDbContext context, string userName)
    {
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}@test.com",
            PasswordHash = "hash",
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /* Seeds a request directly against the context, bypassing SendFriendReqAsync,
     * for setting up states the service itself refuses to create (e.g. a
     * Friendship existing alongside a still-Pending request). */
    protected static async Task<FriendRequest> SeedFriendRequestAsync(
        AppDbContext context,
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
        context.FriendRequests.Add(request);
        await context.SaveChangesAsync();
        return request;
    }

    protected static async Task<Friendship> SeedFriendshipAsync(
        AppDbContext context,
        Guid userIdAlpha,
        Guid userIdBeta
    )
    {
        var friendship = new Friendship { UserId1 = userIdAlpha, UserId2 = userIdBeta };
        await new FriendshipRepository(context).CreateFriendshipAsync(friendship);
        return friendship;
    }
}
