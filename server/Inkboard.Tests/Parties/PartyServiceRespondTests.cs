using Inkboard.Application.Interfaces;
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
        var party = await Service.CreatePartyAsync(leader.Id);
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        Assert.AreEqual(InviteStatus.Accepted, result.InviteStatus);

        var member = await Context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == invited.Id);
        Assert.IsNotNull(member);
        Assert.AreEqual(UserRole.Member, member.Role);
    }

    [TestMethod]
    public async Task RespondToInvite_Decline_MarksInviteDeclined_UserNotAdded()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var party = await Service.CreatePartyAsync(leader.Id);
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var result = await Service.RespondToUserInviteAsync(invite.Id, invited.Id, false);

        Assert.AreEqual(InviteStatus.Declined, result.InviteStatus);

        var member = await Context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == invited.Id);
        Assert.IsNull(member);
    }

    [TestMethod]
    public async Task RespondToInvite_InviteNotFound_ThrowsPartyNotFoundException()
    {
        var user = await SeedUserAsync(Context, "user");

        var ex = await AssertThrowsAsync<PartyNotFoundException>(() =>
            Service.RespondToUserInviteAsync(Guid.NewGuid(), user.Id, true));

        Assert.AreEqual("Invite not found.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_NotTheInvitedUser_ThrowsPartyForbiddenException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var other = await SeedUserAsync(Context, "other");
        var party = await Service.CreatePartyAsync(leader.Id);
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var ex = await AssertThrowsAsync<PartyForbiddenException>(() =>
            Service.RespondToUserInviteAsync(invite.Id, other.Id, true));

        Assert.AreEqual("This invite does not belong to you.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_AlreadyResponded_ThrowsPartyValidationException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var party = await Service.CreatePartyAsync(leader.Id);
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.RespondToUserInviteAsync(invite.Id, invited.Id, true));

        Assert.AreEqual("This invite has already been responded to.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_ExpiredInvite_ThrowsPartyValidationException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var party = await Service.CreatePartyAsync(leader.Id);

        var invite = new PartyInvite
        {
            PartyId = party.Id,
            InvitedByUserId = leader.Id,
            InvitedUserId = invited.Id,
            InviteStatus = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        };
        Context.PartyInvites.Add(invite);
        await Context.SaveChangesAsync();

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.RespondToUserInviteAsync(invite.Id, invited.Id, true));

        Assert.AreEqual("This invite has expired.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_InviterBlockedInviteeAfterSending_ThrowsPartyValidationException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var party = await Service.CreatePartyAsync(leader.Id);
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        await Service.BlockUserAsync(leader.Id, invited.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.RespondToUserInviteAsync(invite.Id, invited.Id, true));

        Assert.AreEqual("You are no longer able to join this party.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_PartyFilledWhileInvitePending_ThrowsPartyValidationException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var delayed = await SeedUserAsync(Context, "delayed");
        var party = await Service.CreatePartyAsync(leader.Id);
        var delayedInvite = await Service.InviteUserAsync(party.Id, leader.Id, delayed.Id);

        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync(Context, $"member{i}");
            var inv = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await Service.RespondToUserInviteAsync(inv.Id, m.Id, true);
        }

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.RespondToUserInviteAsync(delayedInvite.Id, delayed.Id, true));

        Assert.AreEqual("Party is full.", ex.Message);
    }
}
