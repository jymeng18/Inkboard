using Inkboard.Domain.Models;

namespace Inkboard.Tests.Friends;

[TestClass]
public sealed class FriendsServiceGetListsTests : FriendsTestBase
{
    [TestMethod]
    public async Task GetFriendsList_UserOnLowerAndHigherSideOfDifferentPairs_ReturnsBoth()
    {
        var subject = await SeedUserAsync(Context, "subject");
        var lowerFriend = await SeedUserAsync(Context, "aaa_lower");
        var higherFriend = await SeedUserAsync(Context, "zzz_higher");
        // One pair lands subject on the low side, the other on the high side,
        // depending purely on random Guid comparison, so seed both directions
        // through CreateFriendshipAsync so it normalizes for us either way.
        await SeedFriendshipAsync(Context, subject.Id, lowerFriend.Id);
        await SeedFriendshipAsync(Context, higherFriend.Id, subject.Id);

        var result = await Service.GetFriendsListByIdAsync(subject.Id);

        Assert.IsTrue(result.IsSuccess);
        var names = result.Data!.ConvertAll(f => f.UserName);
        CollectionAssert.AreEquivalent(
            new[] { lowerFriend.UserName, higherFriend.UserName },
            names
        );
    }

    [TestMethod]
    public async Task GetPendingRequests_OnlyReturnsRequestsStillPending()
    {
        var receiver = await SeedUserAsync(Context, "receiver");
        var pendingSender = await SeedUserAsync(Context, "pendingSender");
        var declinedSender = await SeedUserAsync(Context, "declinedSender");
        var acceptedSender = await SeedUserAsync(Context, "acceptedSender");

        var pending = await Service.SendFriendReqAsync(pendingSender.Id, receiver.Id);
        Assert.IsTrue(pending.IsSuccess);

        var declined = await Service.SendFriendReqAsync(declinedSender.Id, receiver.Id);
        var rejected = await Service.RejectFriendReqAsync(
            declined.Data!.Id,
            declinedSender.Id,
            receiver.Id
        );
        Assert.IsTrue(rejected.IsSuccess);

        var accepted = await Service.SendFriendReqAsync(acceptedSender.Id, receiver.Id);
        var acceptedResult = await Service.AcceptFriendReqAsync(
            accepted.Data!.Id,
            acceptedSender.Id,
            receiver.Id
        );
        Assert.IsTrue(acceptedResult.IsSuccess);

        var result = await Service.GetPendingRequestsByIdAsync(receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        var senderIds = result.Data!.ConvertAll(fr => fr.UserId);
        CollectionAssert.AreEquivalent(new[] { pendingSender.Id }, senderIds);
    }

    [TestMethod]
    public async Task GetAllRequests_ReturnsEveryStatusForThatReceiver()
    {
        var receiver = await SeedUserAsync(Context, "receiver");
        var pendingSender = await SeedUserAsync(Context, "pendingSender");
        var declinedSender = await SeedUserAsync(Context, "declinedSender");

        var pending = await Service.SendFriendReqAsync(pendingSender.Id, receiver.Id);
        Assert.IsTrue(pending.IsSuccess);

        var declined = await Service.SendFriendReqAsync(declinedSender.Id, receiver.Id);
        var rejected = await Service.RejectFriendReqAsync(
            declined.Data!.Id,
            declinedSender.Id,
            receiver.Id
        );
        Assert.IsTrue(rejected.IsSuccess);

        var result = await Service.GetAllRequestsByIdAsync(receiver.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.Data!.Count);
        var statusesBySender = result.Data.ToDictionary(fr => fr.UserId, fr => fr.Status);
        Assert.AreEqual(RequestStatus.Pending, statusesBySender[pendingSender.Id]);
        Assert.AreEqual(RequestStatus.Declined, statusesBySender[declinedSender.Id]);
    }
}
