using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceRespondEdgeTests : PartyTestBase
{
    private async Task<(Party Party, User Leader)> SeedActiveParty(string leaderName = "leader")
    {
        var leader = await SeedUserAsync(leaderName);
        var canvas = await SeedCanvasAsync(leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        return (partyResult.Data!, leader);
    }

    private async Task<PartyInvite> SeedRawInvite(
        Guid partyId,
        Guid leaderId,
        Guid invitedUserId,
        InviteStatus status,
        DateTimeOffset expiresAt
    )
    {
        var invite = new PartyInvite
        {
            PartyId = partyId,
            InvitedByUserId = leaderId,
            InvitedUserId = invitedUserId,
            InviteStatus = status,
            ExpiresAt = expiresAt,
        };
        Context.PartyInvites.Add(invite);
        await Context.SaveChangesAsync();
        return invite;
    }

    [TestMethod]
    public async Task RespondToInvite_DeclineExpiredInvite_ReturnsExpired()
    {
        // Expiry is checked before the accept/decline branch, so even a decline is rejected.
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var invite = await SeedRawInvite(
            party.Id,
            leader.Id,
            invited.Id,
            InviteStatus.Pending,
            DateTimeOffset.UtcNow.AddMinutes(-1)
        );

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, false);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("This invite has expired.", result.Error);
    }

    [TestMethod]
    public async Task RespondToInvite_DeclineAlreadyDeclined_ReturnsConflict()
    {
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, invited.Id, false);

        var result = await Service.RespondToUserInviteAsync(
            inviteResult.Data.Id,
            invited.Id,
            false
        );

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("This invite has already been responded to.", result.Error);
    }

    [TestMethod]
    public async Task RespondToInvite_AcceptAfterDecline_ReturnsConflict()
    {
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, invited.Id, false);

        var result = await Service.RespondToUserInviteAsync(inviteResult.Data.Id, invited.Id, true);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
    }

    [TestMethod]
    public async Task RespondToInvite_DeclineDoesNotCheckBlockList()
    {
        // Declining while blocked still succeeds (block re-check only guards acceptance).
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.BlockUserAsync(leader.Id, invited.Id);

        var result = await Service.RespondToUserInviteAsync(
            inviteResult.Data!.Id,
            invited.Id,
            false
        );

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(InviteStatus.Declined, result.Data!.InviteStatus);
    }

    [TestMethod]
    public async Task RespondToInvite_DeclineDoesNotCheckMemberCap()
    {
        // A full party does not stop the invitee from declining.
        var (party, leader) = await SeedActiveParty();
        var delayed = await SeedUserAsync("delayed");
        var delayedInvite = await Service.InviteUserAsync(party.Id, leader.Id, delayed.Id);
        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync($"member{i}");
            var inv = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await Service.RespondToUserInviteAsync(inv.Data!.Id, m.Id, true);
        }

        var result = await Service.RespondToUserInviteAsync(
            delayedInvite.Data!.Id,
            delayed.Id,
            false
        );

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(InviteStatus.Declined, result.Data!.InviteStatus);
    }

    [TestMethod]
    public async Task RespondToInvite_AcceptAssignsMemberRole_NotLeader()
    {
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, invited.Id, true);

        var member = await Context.PartyMembers.FirstAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == invited.Id
        );
        Assert.AreEqual(UserRole.Member, member.Role);
    }

    [TestMethod]
    public async Task RespondToInvite_UserCanAcceptInvitesToTwoDifferentParties()
    {
        // There is no cross-party guard on acceptance: a user can belong to multiple parties.
        var (partyA, leaderA) = await SeedActiveParty("leaderA");
        var (partyB, leaderB) = await SeedActiveParty("leaderB");
        var joiner = await SeedUserAsync("joiner");

        var inviteA = await Service.InviteUserAsync(partyA.Id, leaderA.Id, joiner.Id);
        var acceptA = await Service.RespondToUserInviteAsync(inviteA.Data!.Id, joiner.Id, true);
        var inviteB = await Service.InviteUserAsync(partyB.Id, leaderB.Id, joiner.Id);
        var acceptB = await Service.RespondToUserInviteAsync(inviteB.Data!.Id, joiner.Id, true);

        Assert.IsTrue(acceptA.IsSuccess);
        Assert.IsFalse(acceptB.IsSuccess);
        var partyCount = await Context.PartyMembers.CountAsync(pm => pm.UserId == joiner.Id);
        Assert.AreEqual(1, partyCount);
    }

    [TestMethod]
    public async Task RespondToInvite_AcceptWhileBlocked_DoesNotAddMember()
    {
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.BlockUserAsync(leader.Id, invited.Id);

        var result = await Service.RespondToUserInviteAsync(
            inviteResult.Data!.Id,
            invited.Id,
            true
        );

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        var isMember = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == invited.Id
        );
        Assert.IsFalse(isMember);
    }

    [TestMethod]
    public async Task RespondToInvite_AcceptWhileBlocked_LeavesInviteStillPending()
    {
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.BlockUserAsync(leader.Id, invited.Id);

        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, invited.Id, true);

        var invite = await Context.PartyInvites.FirstAsync(pi => pi.Id == inviteResult.Data.Id);
        Assert.AreEqual(InviteStatus.Pending, invite.InviteStatus);
    }

    [TestMethod]
    public async Task RespondToInvite_AcceptWhenPartyAlreadyAtFive_DoesNotAddMember()
    {
        var (party, leader) = await SeedActiveParty();
        var delayed = await SeedUserAsync("delayed");
        var delayedInvite = await Service.InviteUserAsync(party.Id, leader.Id, delayed.Id);
        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync($"member{i}");
            var inv = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await Service.RespondToUserInviteAsync(inv.Data!.Id, m.Id, true);
        }

        var result = await Service.RespondToUserInviteAsync(
            delayedInvite.Data!.Id,
            delayed.Id,
            true
        );

        Assert.IsFalse(result.IsSuccess);
        var isMember = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == delayed.Id
        );
        Assert.IsFalse(isMember);
    }

    [TestMethod]
    public async Task RespondToInvite_ExpiredInvite_AcceptDoesNotAddMember()
    {
        var (party, leader) = await SeedActiveParty();
        var invited = await SeedUserAsync("invited");
        var invite = await SeedRawInvite(
            party.Id,
            leader.Id,
            invited.Id,
            InviteStatus.Pending,
            DateTimeOffset.UtcNow.AddSeconds(-1)
        );

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        Assert.IsFalse(result.IsSuccess);
        var isMember = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == invited.Id
        );
        Assert.IsFalse(isMember);
    }
}
