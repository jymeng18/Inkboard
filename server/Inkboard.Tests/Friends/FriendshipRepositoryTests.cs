using Inkboard.Domain.Models;
using Inkboard.Infra.Db;

namespace Inkboard.Tests.Friends;

/*
 * Friendships are stored one row per pair with the smaller id first, so a pair
 * can never be recorded twice. These pin down the normalization that upholds
 * it, plus the directional lookup FriendRequestRepository relies on for the
 * crossing-request rule.
 */
[TestClass]
public sealed class FriendshipRepositoryTests : FriendsTestBase
{
    [TestMethod]
    public async Task CreateFriendship_PairGivenInReverseOrder_StoresSameCanonicalRow()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var repository = new FriendshipRepository(Context);

        await repository.CreateFriendshipAsync(new Friendship { UserId1 = alpha, UserId2 = beta });

        // A second insert with the pair flipped must land on the same row, or
        // the check constraint would have rejected it outright on real Postgres.
        var stored = Context.Friendships.Single();
        Assert.IsTrue(
            (stored.UserId1 == alpha && stored.UserId2 == beta)
                || (stored.UserId1 == beta && stored.UserId2 == alpha)
        );
        Assert.AreEqual(1, Context.Friendships.Count());
    }

    [TestMethod]
    public async Task CreateFriendship_StoredOrderAlwaysSatisfiesUuidByteOrderCheckConstraint()
    {
        // CK_Friendships_UserOrder compares raw uuid bytes, not Guid.CompareTo.
        // Whichever id CreateFriendshipAsync decides is "first", it must be the
        // one Postgres would also call smaller, on every pair, not just ones
        // hand picked to already agree.
        var rng = new Random(2024);
        for (int i = 0; i < 200; i++)
        {
            var alpha = NextGuid(rng);
            var beta = NextGuid(rng);
            if (alpha == beta)
                continue;

            using var context = CreateDbContext();
            var repository = new FriendshipRepository(context);
            await repository.CreateFriendshipAsync(
                new Friendship { UserId1 = alpha, UserId2 = beta }
            );

            var stored = context.Friendships.Single();
            var byteOrder = stored
                .UserId1.ToByteArray(bigEndian: true)
                .AsSpan()
                .SequenceCompareTo(stored.UserId2.ToByteArray(bigEndian: true));
            Assert.IsLessThan(
                0,
                byteOrder,
                $"UserId1 {stored.UserId1} must sort before UserId2 {stored.UserId2} in uuid byte order."
            );
        }
    }

    private static Guid NextGuid(Random rng)
    {
        var bytes = new byte[16];
        rng.NextBytes(bytes);
        return new Guid(bytes);
    }

    [TestMethod]
    public async Task GetPendingBetweenAsync_IsDirectional_DoesNotMatchReverseDirection()
    {
        var alice = await SeedUserAsync("alice");
        var bob = await SeedUserAsync("bob");
        await SeedFriendRequestAsync(alice.Id, bob.Id);
        var repository = new FriendRequestRepository(Context);

        // Alice sent Bob a request. Looking it up as if Bob sent Alice one must
        // find nothing, this is exactly what the crossing-request check in
        // SendFriendReqAsync depends on to tell the two cases apart.
        var reverseLookup = await repository.GetPendingBetweenAsync(bob.Id, alice.Id);
        var forwardLookup = await repository.GetPendingBetweenAsync(alice.Id, bob.Id);

        Assert.IsNull(reverseLookup);
        Assert.IsNotNull(forwardLookup);
    }
}
