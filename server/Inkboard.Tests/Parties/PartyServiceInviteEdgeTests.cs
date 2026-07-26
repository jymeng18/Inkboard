using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceInviteEdgeTests : PartyTestBase
{
    [TestMethod]
    public async Task InviteUser_ReInviteAfterDecline_CreatesNewPendingInvite()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var first = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.RespondToUserInviteAsync(first.Data!.Id, invited.Id, false);

        var second = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        Assert.IsTrue(second.IsSuccess);
        Assert.AreNotEqual(first.Data.Id, second.Data!.Id);
        Assert.AreEqual(InviteStatus.Pending, second.Data.InviteStatus);
    }

    [TestMethod]
    public async Task InviteUser_ReInviteAfterMemberLeft_Succeeds()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Data!.Id, member.Id, true);
        await Service.LeavePartyAsync(party.Id, member.Id);

        var result = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(InviteStatus.Pending, result.Data!.InviteStatus);
    }

    [TestMethod]
    public async Task InviteUser_ReInviteAfterKicked_Succeeds()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Data!.Id, member.Id, true);
        await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);

        var result = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task InviteUser_PendingInvitesDoNotCountTowardMemberCap()
    {
        // Party has only the leader (1 member). Sending many pending invites is allowed
        // because the cap check counts actual members, not outstanding invites.
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;

        for (int i = 0; i < 8; i++)
        {
            var u = await SeedUserAsync(Context, $"pending{i}");
            var result = await Service.InviteUserAsync(party.Id, leader.Id, u.Id);
            Assert.IsTrue(result.IsSuccess, $"Invite {i} should succeed while pending.");
        }

        var pendingCount = await Context.PartyInvites.CountAsync(pi =>
            pi.PartyId == party.Id && pi.InviteStatus == InviteStatus.Pending
        );
        Assert.AreEqual(8, pendingCount);
    }

    [TestMethod]
    public async Task InviteUser_InviteeHasBlockedLeader_LeaderCanStillInvite()
    {
        // Block list is directional: the invitee blocking the leader does not stop the leader.
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;

        var blockResult = await Service.BlockUserAsync(invited.Id, leader.Id);
        Assert.IsTrue(blockResult.IsSuccess);

        var result = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task InviteUser_NonexistentInvitedUser_StillCreatesInvite()
    {
        // The service does not verify the invited user actually exists.
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;

        var result = await Service.InviteUserAsync(party.Id, leader.Id, Guid.NewGuid());

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task InviteUser_MultipleDistinctUsers_EachGetIndependentPendingInvite()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var a = await SeedUserAsync(Context, "a");
        var b = await SeedUserAsync(Context, "b");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;

        var inviteA = await Service.InviteUserAsync(party.Id, leader.Id, a.Id);
        var inviteB = await Service.InviteUserAsync(party.Id, leader.Id, b.Id);

        Assert.IsTrue(inviteA.IsSuccess);
        Assert.IsTrue(inviteB.IsSuccess);
        Assert.AreNotEqual(inviteA.Data!.Id, inviteB.Data!.Id);
        Assert.AreEqual(a.Id, inviteA.Data.InvitedUserId);
        Assert.AreEqual(b.Id, inviteB.Data.InvitedUserId);
    }

    [TestMethod]
    public async Task InviteUser_FullPartyBlocksInvite_EvenWithNoPending()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync(Context, $"member{i}");
            var inv = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await Service.RespondToUserInviteAsync(inv.Data!.Id, m.Id, true);
        }

        var extra = await SeedUserAsync(Context, "extra");
        var result = await Service.InviteUserAsync(party.Id, leader.Id, extra.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task InviteUser_MemberOfPartyCannotInvite_ReturnsForbidden()
    {
        // Only the leader may invite; a regular member cannot.
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var target = await SeedUserAsync(Context, "target");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var inv = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(inv.Data!.Id, member.Id, true);

        var result = await Service.InviteUserAsync(party.Id, member.Id, target.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Only the leader can invite people.", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_PreviouslyDeclinedInviteRemainsDeclined_AfterReInvite()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var first = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.RespondToUserInviteAsync(first.Data!.Id, invited.Id, false);

        await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var oldInvite = await Context.PartyInvites.FirstAsync(pi => pi.Id == first.Data.Id);
        Assert.AreEqual(InviteStatus.Declined, oldInvite.InviteStatus);
    }
}
