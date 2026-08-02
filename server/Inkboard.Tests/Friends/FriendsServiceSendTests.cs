using Inkboard.Application.Common;
using Inkboard.Domain.Models;

namespace Inkboard.Tests.Friends;

[TestClass]
public sealed class FriendsServiceSendTests : FriendsTestBase
{
    [TestMethod]
    public async Task Send_ValidRequest_CreatesPendingRequestAndReturnsReceiverDto()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");

        var result = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        var dto = result.Data!;
        Assert.AreEqual(receiver.Id, dto.UserId);
        Assert.AreEqual(receiver.UserName, dto.UserName);
        Assert.AreEqual(RequestStatus.Pending, dto.Status);

        var stored = Context.FriendRequests.Single();
        Assert.AreEqual(sender.Id, stored.RequesterId);
        Assert.AreEqual(receiver.Id, stored.RequesteeId);
        Assert.AreEqual(RequestStatus.Pending, stored.Status);
    }

    [TestMethod]
    public async Task Send_ToSelf_ReturnsValidationError()
    {
        var user = await SeedUserAsync(Context, "user");

        var result = await Service.SendFriendReqAsync(user.Id, user.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You cannot send a friend request to yourself.", result.Error);
    }

    [TestMethod]
    public async Task Send_ToNonexistentUser_ReturnsNotFound()
    {
        var sender = await SeedUserAsync(Context, "sender");

        var result = await Service.SendFriendReqAsync(sender.Id, Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("User does not exist.", result.Error);
    }

    [TestMethod]
    public async Task Send_AlreadyFriends_ReturnsValidationError()
    {
        var alpha = await SeedUserAsync(Context, "alpha");
        var beta = await SeedUserAsync(Context, "beta");
        await SeedFriendshipAsync(Context, alpha.Id, beta.Id);

        var result = await Service.SendFriendReqAsync(alpha.Id, beta.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You're already friends.", result.Error);
    }

    [TestMethod]
    public async Task Send_DuplicatePendingFromSameSender_ReturnsConflict()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var first = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        Assert.IsTrue(first.IsSuccess);

        var result = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("You have already sent this user a friend request.", result.Error);
    }

    [TestMethod]
    public async Task Send_ReceiverAlreadySentPendingRequest_ReturnsConflict()
    {
        var alice = await SeedUserAsync(Context, "alice");
        var bob = await SeedUserAsync(Context, "bob");
        // Bob already sent Alice a request. Alice tries to send one to Bob instead
        // of just answering the one waiting in her inbox.
        var bobToAlice = await Service.SendFriendReqAsync(bob.Id, alice.Id);
        Assert.IsTrue(bobToAlice.IsSuccess);

        var result = await Service.SendFriendReqAsync(alice.Id, bob.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual(
            "Check your inbox. This user has sent you a friend request already.",
            result.Error
        );
    }

    [TestMethod]
    public async Task Send_AfterPreviousRequestWasDeclined_CreatesNewPendingRequest()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var first = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var rejected = await Service.RejectFriendReqAsync(first.Data!.Id, sender.Id, receiver.Id);
        Assert.IsTrue(rejected.IsSuccess);

        var result = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, Context.FriendRequests.Count());
    }

    [TestMethod]
    public async Task Send_AfterPreviousRequestWasRevoked_CreatesNewPendingRequest()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var first = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var cancelled = await Service.CancelFriendReqAsync(first.Data!.Id, sender.Id);
        Assert.IsTrue(cancelled.IsSuccess);

        var result = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, Context.FriendRequests.Count());
    }

    [TestMethod]
    public async Task Send_UnrelatedPendingRequestBetweenOtherUsers_DoesNotBlock()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var carol = await SeedUserAsync(Context, "carol");
        var dave = await SeedUserAsync(Context, "dave");
        // An unrelated pending request exists elsewhere in the table. It must not
        // leak into an unrelated pair's check.
        var unrelated = await Service.SendFriendReqAsync(carol.Id, dave.Id);
        Assert.IsTrue(unrelated.IsSuccess);

        var result = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        Assert.IsTrue(result.IsSuccess);
    }
}
