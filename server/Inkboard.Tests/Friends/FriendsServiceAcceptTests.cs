using Inkboard.Application.Common;
using Inkboard.Domain.Models;

namespace Inkboard.Tests.Friends;

[TestClass]
public sealed class FriendsServiceAcceptTests : FriendsTestBase
{
    [TestMethod]
    public async Task Accept_ValidRequest_CreatesFriendshipMarksRequestAcceptedAndReturnsSenderDto()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        var result = await Service.AcceptFriendReqAsync(sent.Data!.Id, sender.Id, receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        var dto = result.Data!;
        Assert.AreEqual(sender.Id, dto.UserId);
        Assert.AreEqual(sender.UserName, dto.UserName);
        Assert.AreEqual(RequestStatus.Accepted, dto.Status);

        var storedRequest = Context.FriendRequests.Single(fr => fr.Id == sent.Data.Id);
        Assert.AreEqual(RequestStatus.Accepted, storedRequest.Status);

        var friendship = await new Inkboard.Infra.Db.FriendshipRepository(Context).GetByIdAsync(
            sender.Id,
            receiver.Id
        );
        Assert.IsNotNull(friendship);
    }

    [TestMethod]
    public async Task Accept_RequestNotFound_ReturnsNotFound()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");

        var result = await Service.AcceptFriendReqAsync(Guid.NewGuid(), sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Friend request not found.", result.Error);
    }

    [TestMethod]
    public async Task Accept_ReceiverDoesNotExist_ReturnsNotFound()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        var result = await Service.AcceptFriendReqAsync(sent.Data!.Id, sender.Id, Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("User(receiver) does not exist.", result.Error);
    }

    [TestMethod]
    public async Task Accept_SenderDoesNotExist_ReturnsNotFound()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        var result = await Service.AcceptFriendReqAsync(sent.Data!.Id, Guid.NewGuid(), receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("User(sender) does not exist.", result.Error);
    }

    [TestMethod]
    public async Task Accept_ReceiverMismatch_ReturnsForbidden()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var stranger = await SeedUserAsync(Context, "stranger");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        // Stranger tries to accept a request that was never addressed to them.
        var result = await Service.AcceptFriendReqAsync(sent.Data!.Id, sender.Id, stranger.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task Accept_ForgedSenderId_ReturnsForbidden()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var attacker = await SeedUserAsync(Context, "attacker");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        // Correct requestId and correct receiverId (the real receiver acting on
        // their own inbox item), but a forged senderId that never sent anything.
        // Must not create a friendship between receiver and the forged sender.
        var result = await Service.AcceptFriendReqAsync(sent.Data!.Id, attacker.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);

        var friendshipWithAttacker = await new Inkboard.Infra.Db.FriendshipRepository(
            Context
        ).AreFriendsAsync(attacker.Id, receiver.Id);
        Assert.IsFalse(friendshipWithAttacker);
    }

    [TestMethod]
    public async Task Accept_FriendshipAlreadyExistsForThatPair_ReturnsValidationError()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        // A friendship exists already (e.g. from a previous request cycle) while
        // a fresh Pending request for the same pair also exists.
        await SeedFriendshipAsync(Context, sender.Id, receiver.Id);
        var request = await SeedFriendRequestAsync(Context, sender.Id, receiver.Id);

        var result = await Service.AcceptFriendReqAsync(request.Id, sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You are already friends.", result.Error);
    }

    [TestMethod]
    public async Task Accept_ReplayOfAlreadyAcceptedRequest_Fails()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var firstAccept = await Service.AcceptFriendReqAsync(
            sent.Data!.Id,
            sender.Id,
            receiver.Id
        );
        Assert.IsTrue(firstAccept.IsSuccess);

        // Replaying the same accept call must not be able to run the create
        // path a second time. Whichever guard catches it, the outcome must fail
        // and the friendship must not be duplicated.
        var result = await Service.AcceptFriendReqAsync(sent.Data.Id, sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(1, Context.Friendships.Count());
    }

    [TestMethod]
    public async Task Accept_AlreadyDeclinedRequest_ReturnsNotFound()
    {
        var sender = await SeedUserAsync(Context, "sender");
        var receiver = await SeedUserAsync(Context, "receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var rejected = await Service.RejectFriendReqAsync(
            sent.Data!.Id,
            sender.Id,
            receiver.Id
        );
        Assert.IsTrue(rejected.IsSuccess);

        var result = await Service.AcceptFriendReqAsync(sent.Data.Id, sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.IsFalse(Context.Friendships.Any());
    }
}
