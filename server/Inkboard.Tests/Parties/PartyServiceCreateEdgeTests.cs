using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceCreateEdgeTests : PartyTestBase
{
    [TestMethod]
    public async Task CreateParty_UserIsMemberOfAnotherParty_ReturnsConflict()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, member.Id, true);

        // The member is not a leader, but is already in an active party.
        var ownCanvas = await SeedCanvasAsync(Context, member.Id, "Member Canvas");
        var result = await Service.CreatePartyAsync(member.Id, ownCanvas.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("An active party already exists.", result.Error);
    }

    [TestMethod]
    public async Task CreateParty_DoesNotCreateAnyInvite()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);

        var result = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(result.IsSuccess);

        var anyInvites = await Context.PartyInvites
            .AnyAsync(pi => pi.PartyId == result.Data!.Id);
        Assert.IsFalse(anyInvites);
    }

    [TestMethod]
    public async Task CreateParty_SetsCreatedAtToApproximatelyNow()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(result.IsSuccess);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.IsTrue(result.Data!.CreatedAt >= before);
        Assert.IsTrue(result.Data.CreatedAt <= after);
    }

    [TestMethod]
    public async Task CreateParty_TwoDifferentUsers_CreateIndependentParties()
    {
        var leaderA = await SeedUserAsync(Context, "leaderA");
        var leaderB = await SeedUserAsync(Context, "leaderB");
        var canvasA = await SeedCanvasAsync(Context, leaderA.Id, "A");
        var canvasB = await SeedCanvasAsync(Context, leaderB.Id, "B");

        var resultA = await Service.CreatePartyAsync(leaderA.Id, canvasA.Id);
        var resultB = await Service.CreatePartyAsync(leaderB.Id, canvasB.Id);

        Assert.IsTrue(resultA.IsSuccess);
        Assert.IsTrue(resultB.IsSuccess);
        Assert.AreNotEqual(resultA.Data!.Id, resultB.Data!.Id);
    }

    [TestMethod]
    public async Task CreateParty_WithNonexistentCanvasId_Fails()
    {
        // The service does not validate that the canvas exists.
        var leader = await SeedUserAsync(Context, "leader");

        var result = await Service.CreatePartyAsync(leader.Id, Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task CreateParty_LeaderIsOnlyMemberInitially()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);

        var result = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(result.IsSuccess);

        var count = await Context.PartyMembers.CountAsync(pm => pm.PartyId == result.Data!.Id);
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public async Task CreateParty_AfterLeavingSoleParty_CanCreateAgain()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var first = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(first.IsSuccess);
        await Service.LeavePartyAsync(first.Data!.Id, leader.Id);

        var canvas2 = await SeedCanvasAsync(Context, leader.Id, "Second");
        var second = await Service.CreatePartyAsync(leader.Id, canvas2.Id);

        Assert.IsTrue(second.IsSuccess);
        Assert.AreNotEqual(first.Data.Id, second.Data!.Id);
    }

    [TestMethod]
    public async Task CreateParty_NotifiesMemberJoinedForLeader()
    {
        using var context = CreateDbContext();
        var service = CreatePartyServiceWithNotifier(context, out var notifier);
        var leader = await SeedUserAsync(context, "leader");
        var canvas = await SeedCanvasAsync(context, leader.Id);

        var result = await service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(result.IsSuccess);

        notifier.Verify(
            n => n.NotifyMemberJoined(
                result.Data!.Id,
                It.Is<PartyMember>(m => m.UserId == leader.Id && m.Role == UserRole.Leader)),
            Times.Once);
    }
}
