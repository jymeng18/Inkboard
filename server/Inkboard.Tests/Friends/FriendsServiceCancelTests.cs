using Inkboard.Application.Common;
using Inkboard.Domain.Models;

namespace Inkboard.Tests.Friends;

[TestClass]
public sealed class FriendsServiceCancelTests : FriendsTestBase
{
    [TestMethod]
    public async Task Cancel_ValidPendingRequest_MarksRevoked()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        var result = await Service.CancelFriendReqAsync(sent.Data!.Id, sender.Id);

        Assert.IsTrue(result.IsSuccess);
        var stored = Context.FriendRequests.Single(fr => fr.Id == sent.Data.Id);
        Assert.AreEqual(RequestStatus.Revoked, stored.Status);
    }

    [TestMethod]
    public async Task Cancel_RequestNotFound_ReturnsNotFound()
    {
        var sender = await SeedUserAsync(Context, "sender");

        var result = await Service.CancelFriendReqAsync(Guid.NewGuid(), sender.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Friend request not found.", result.Error);
    }

    [TestMethod]
    public async Task Cancel_ReceiverAttemptsToCancel_ReturnsForbidden()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        // Cancelling is the sender's move. The receiver should reject, not cancel.
        var result = await Service.CancelFriendReqAsync(sent.Data!.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("You are not the sender!", result.Error);

        var stored = Context.FriendRequests.Single(fr => fr.Id == sent.Data.Id);
        Assert.AreEqual(RequestStatus.Pending, stored.Status);
    }

    [TestMethod]
    public async Task Cancel_AlreadyAccepted_ReturnsConflictAndFriendshipUnaffected()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var accepted = await Service.AcceptFriendReqAsync(
            sent.Data!.Id,
            sender.Id,
            receiver.Id
        );
        Assert.IsTrue(accepted.IsSuccess);

        var result = await Service.CancelFriendReqAsync(sent.Data.Id, sender.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual(1, Context.Friendships.Count());
    }

    [TestMethod]
    public async Task Cancel_AlreadyRevoked_ReturnsConflict()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var first = await Service.CancelFriendReqAsync(sent.Data!.Id, sender.Id);
        Assert.IsTrue(first.IsSuccess);

        var result = await Service.CancelFriendReqAsync(sent.Data.Id, sender.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
    }
}
