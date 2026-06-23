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
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var result = await service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        Assert.AreEqual(InviteStatus.Accepted, result.InviteStatus);

        var member = await context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == invited.Id);
        Assert.IsNotNull(member);
        Assert.AreEqual(UserRole.Member, member.Role);
    }

    [TestMethod]
    public async Task RespondToInvite_Decline_MarksInviteDeclined_UserNotAdded()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var result = await service.RespondToUserInviteAsync(invite.Id, invited.Id, false);

        Assert.AreEqual(InviteStatus.Declined, result.InviteStatus);

        var member = await context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == invited.Id);
        Assert.IsNull(member);
    }

    [TestMethod]
    public async Task RespondToInvite_InviteNotFound_ThrowsPartyNotFoundException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var user = await SeedUserAsync(context, "user");

        var ex = await AssertThrowsAsync<PartyNotFoundException>(() =>
            service.RespondToUserInviteAsync(Guid.NewGuid(), user.Id, true));

        Assert.AreEqual("Invite not found.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_NotTheInvitedUser_ThrowsPartyForbiddenException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var other = await SeedUserAsync(context, "other");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var ex = await AssertThrowsAsync<PartyForbiddenException>(() =>
            service.RespondToUserInviteAsync(invite.Id, other.Id, true));

        Assert.AreEqual("This invite does not belong to you.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_AlreadyResponded_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await service.RespondToUserInviteAsync(invite.Id, invited.Id, true);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.RespondToUserInviteAsync(invite.Id, invited.Id, true));

        Assert.AreEqual("This invite has already been responded to.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_ExpiredInvite_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);

        var invite = new PartyInvite
        {
            PartyId = party.Id,
            InvitedByUserId = leader.Id,
            InvitedUserId = invited.Id,
            InviteStatus = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        };
        context.PartyInvites.Add(invite);
        await context.SaveChangesAsync();

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.RespondToUserInviteAsync(invite.Id, invited.Id, true));

        Assert.AreEqual("This invite has expired.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_InviterBlockedInviteeAfterSending_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        await service.BlockUserAsync(leader.Id, invited.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.RespondToUserInviteAsync(invite.Id, invited.Id, true));

        Assert.AreEqual("You are no longer able to join this party.", ex.Message);
    }

    [TestMethod]
    public async Task RespondToInvite_PartyFilledWhileInvitePending_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var delayed = await SeedUserAsync(context, "delayed");
        var party = await service.CreatePartyAsync(leader.Id);
        var delayedInvite = await service.InviteUserAsync(party.Id, leader.Id, delayed.Id);

        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync(context, $"member{i}");
            var inv = await service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await service.RespondToUserInviteAsync(inv.Id, m.Id, true);
        }

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.RespondToUserInviteAsync(delayedInvite.Id, delayed.Id, true));

        Assert.AreEqual("Party is full.", ex.Message);
    }
}
