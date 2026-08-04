using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceRespondTests : PartyTestBase
{
    [TestMethod]
    public async Task RespondToInvite_Accept_AddsMemberAndMarksInviteAccepted()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);
        Assert.IsTrue(result.IsSuccess);
        var updatedInvite = result.Data!;

        Assert.AreEqual(InviteStatus.Accepted, updatedInvite.InviteStatus);

        var member = await Context.PartyMembers.FirstOrDefaultAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == invited.Id
        );
        Assert.IsNotNull(member);
        Assert.AreEqual(UserRole.Member, member.Role);
    }

    [TestMethod]
    public async Task RespondToInvite_Decline_MarksInviteDeclined_UserNotAdded()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, false);
        Assert.IsTrue(result.IsSuccess);
        var updatedInvite = result.Data!;

        Assert.AreEqual(InviteStatus.Declined, updatedInvite.InviteStatus);

        var member = await Context.PartyMembers.FirstOrDefaultAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == invited.Id
        );
        Assert.IsNull(member);
    }

    [TestMethod]
    public async Task RespondToInvite_InviteNotFound_ReturnsNotFound()
    {
        var user = await SeedUserAsync(Context, "user");

        var result = await Service.RespondToUserInviteAsync(Guid.NewGuid(), user.Id, true);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Invite not found.", result.Error);
    }

    [TestMethod]
    public async Task RespondToInvite_NotTheInvitedUser_ReturnsForbidden()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var other = await SeedUserAsync(Context, "other");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;

        var result = await Service.RespondToUserInviteAsync(invite.Id, other.Id, true);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("This invite does not belong to you.", result.Error);
    }

    [TestMethod]
    public async Task RespondToInvite_AlreadyResponded_ReturnsConflict()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;

        var firstResult = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);
        Assert.IsTrue(firstResult.IsSuccess);

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("This invite has already been responded to.", result.Error);
    }

    [TestMethod]
    public async Task RespondToInvite_ExpiredInvite_ReturnsValidationError()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var invite = new PartyInvite
        {
            PartyId = party.Id,
            InvitedByUserId = leader.Id,
            InvitedUserId = invited.Id,
            InviteStatus = InviteStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        Context.PartyInvites.Add(invite);
        await Context.SaveChangesAsync();

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("This invite has expired.", result.Error);
    }

    [TestMethod]
    public async Task RespondToInvite_InviterBlockedInviteeAfterSending_ReturnsForbidden()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;

        var blockResult = await Service.BlockUserAsync(leader.Id, invited.Id);
        Assert.IsTrue(blockResult.IsSuccess);

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("You are no longer able to join this party.", result.Error);
    }

    [TestMethod]
    public async Task RespondToInvite_PartyFilledWhileInvitePending_ReturnsValidationError()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var delayed = await SeedUserAsync(Context, "delayed");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var delayedInviteResult = await Service.InviteUserAsync(party.Id, leader.Id, delayed.Id);
        Assert.IsTrue(delayedInviteResult.IsSuccess);
        var delayedInvite = delayedInviteResult.Data!;

        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync(Context, $"member{i}");
            var invResult = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            Assert.IsTrue(invResult.IsSuccess);
            var inv = invResult.Data!;
            var respondResult = await Service.RespondToUserInviteAsync(inv.Id, m.Id, true);
            Assert.IsTrue(respondResult.IsSuccess);
        }

        var result = await Service.RespondToUserInviteAsync(delayedInvite.Id, delayed.Id, true);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("Party is full.", result.Error);
    }
}
