using Inkboard.Application.Common;

namespace Inkboard.Tests.Friends;

[TestClass]
public sealed class FriendsServiceUnfriendTests : FriendsTestBase
{
    [TestMethod]
    public async Task Unfriend_ValidFriendship_DeletesRow()
    {
        var alpha = await SeedUserAsync(Context, "alpha");
        var beta = await SeedUserAsync(Context, "beta");
        await SeedFriendshipAsync(Context, alpha.Id, beta.Id);

        var result = await Service.UnfriendAsync(alpha.Id, beta.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(Context.Friendships.Any());
    }

    [TestMethod]
    public async Task Unfriend_TargetUserDoesNotExist_ReturnsNotFound()
    {
        var alpha = await SeedUserAsync(Context, "alpha");

        var result = await Service.UnfriendAsync(alpha.Id, Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("User does not exist.", result.Error);
    }

    [TestMethod]
    public async Task Unfriend_NoExistingFriendship_ReturnsNotFound()
    {
        var alpha = await SeedUserAsync(Context, "alpha");
        var beta = await SeedUserAsync(Context, "beta");

        var result = await Service.UnfriendAsync(alpha.Id, beta.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Friendship not found.", result.Error);
    }

    [TestMethod]
    public async Task Unfriend_CallerOnEitherSideOfStoredPair_StillDeletes()
    {
        var alpha = await SeedUserAsync(Context, "alpha");
        var beta = await SeedUserAsync(Context, "beta");
        // Friendship created with beta as the sender, so storage order does not
        // necessarily match (alpha, beta) argument order.
        var sent = await Service.SendFriendReqAsync(beta.Id, alpha.Id);
        var accepted = await Service.AcceptFriendReqAsync(sent.Data!.Id, beta.Id, alpha.Id);
        Assert.IsTrue(accepted.IsSuccess);

        // Alpha unfriends beta, arguments passed in the opposite order to how
        // the request/accept flow ran.
        var result = await Service.UnfriendAsync(alpha.Id, beta.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(Context.Friendships.Any());
    }

    [TestMethod]
    public async Task Unfriend_AfterUnfriend_CanSendNewRequestAgain()
    {
        var alpha = await SeedUserAsync(Context, "alpha");
        var beta = await SeedUserAsync(Context, "beta");
        var sent = await Service.SendFriendReqAsync(alpha.Id, beta.Id);
        var accepted = await Service.AcceptFriendReqAsync(sent.Data!.Id, alpha.Id, beta.Id);
        Assert.IsTrue(accepted.IsSuccess);
        var unfriended = await Service.UnfriendAsync(alpha.Id, beta.Id);
        Assert.IsTrue(unfriended.IsSuccess);

        // The old Accepted request must not be mistaken for a live pending one.
        var result = await Service.SendFriendReqAsync(alpha.Id, beta.Id);

        Assert.IsTrue(result.IsSuccess);
    }
}
