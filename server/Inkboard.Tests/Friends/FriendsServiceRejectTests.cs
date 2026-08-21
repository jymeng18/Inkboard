using Inkboard.Application.Common;
using Inkboard.Domain.Models;

namespace Inkboard.Tests.Friends;

[TestClass]
public sealed class FriendsServiceRejectTests : FriendsTestBase
{
    [TestMethod]
    public async Task Reject_ValidRequest_MarksDeclinedAndReturnsSenderDto()
    {
        var sender = await SeedUserAsync("sender");
        var receiver = await SeedUserAsync("receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        var result = await Service.RejectFriendReqAsync(sent.Data!.Id, sender.Id, receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        var dto = result.Data!;
        Assert.AreEqual(sender.Id, dto.UserId);
        Assert.AreEqual(sender.UserName, dto.UserName);
        Assert.AreEqual(RequestStatus.Declined, dto.Status);

        var stored = Context.FriendRequests.Single(fr => fr.Id == sent.Data.Id);
        Assert.AreEqual(RequestStatus.Declined, stored.Status);
    }

    [TestMethod]
    public async Task Reject_DoesNotCreateFriendship()
    {
        var sender = await SeedUserAsync("sender");
        var receiver = await SeedUserAsync("receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        var result = await Service.RejectFriendReqAsync(sent.Data!.Id, sender.Id, receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(Context.Friendships.Any());
    }

    [TestMethod]
    public async Task Reject_RequestNotFound_ReturnsNotFound()
    {
        var sender = await SeedUserAsync("sender");
        var receiver = await SeedUserAsync("receiver");

        var result = await Service.RejectFriendReqAsync(Guid.NewGuid(), sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Friend request not found.", result.Error);
    }

    [TestMethod]
    public async Task Reject_IdentityMismatch_ReturnsForbidden()
    {
        var sender = await SeedUserAsync("sender");
        var receiver = await SeedUserAsync("receiver");
        var stranger = await SeedUserAsync("stranger");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);

        // Stranger, not the real receiver, tries to decline it.
        var result = await Service.RejectFriendReqAsync(sent.Data!.Id, sender.Id, stranger.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task Reject_AlreadyAccepted_ReturnsNotFound()
    {
        var sender = await SeedUserAsync("sender");
        var receiver = await SeedUserAsync("receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var accepted = await Service.AcceptFriendReqAsync(
            sent.Data!.Id,
            sender.Id,
            receiver.Id
        );
        Assert.IsTrue(accepted.IsSuccess);

        var result = await Service.RejectFriendReqAsync(sent.Data.Id, sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);

        // The friendship the accept created must survive an attempted reject.
        var friendship = Context.FriendRequests.Single(fr => fr.Id == sent.Data.Id);
        Assert.AreEqual(RequestStatus.Accepted, friendship.Status);
    }

    [TestMethod]
    public async Task Reject_AlreadyDeclined_ReturnsNotFound()
    {
        var sender = await SeedUserAsync("sender");
        var receiver = await SeedUserAsync("receiver");
        var sent = await Service.SendFriendReqAsync(sender.Id, receiver.Id);
        var first = await Service.RejectFriendReqAsync(sent.Data!.Id, sender.Id, receiver.Id);
        Assert.IsTrue(first.IsSuccess);

        var result = await Service.RejectFriendReqAsync(sent.Data.Id, sender.Id, receiver.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }
}
