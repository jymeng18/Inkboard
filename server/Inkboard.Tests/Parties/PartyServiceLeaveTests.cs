using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceLeaveTests : PartyTestBase
{
    [TestMethod]
    public async Task LeaveParty_NonLeaderLeaves_RemovedFromParty_PartyRemains()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var member = await SeedUserAsync(context, "member");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await service.RespondToUserInviteAsync(invite.Id, member.Id, true);

        await service.LeavePartyAsync(party.Id, member.Id);

        var isMember = await context.PartyMembers
            .AnyAsync(pm => pm.PartyId == party.Id && pm.UserId == member.Id);
        Assert.IsFalse(isMember);

        var partyExists = await context.Parties.AnyAsync(p => p.Id == party.Id);
        Assert.IsTrue(partyExists);
    }

    [TestMethod]
    public async Task LeaveParty_PartyNotFound_ThrowsPartyNotFoundException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var user = await SeedUserAsync(context, "user");

        var ex = await AssertThrowsAsync<PartyNotFoundException>(() =>
            service.LeavePartyAsync(Guid.NewGuid(), user.Id));

        Assert.AreEqual("Party not found.", ex.Message);
    }

    [TestMethod]
    public async Task LeaveParty_NotAMember_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var nonMember = await SeedUserAsync(context, "nonMember");
        var party = await service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.LeavePartyAsync(party.Id, nonMember.Id));

        Assert.AreEqual("You are not in a party.", ex.Message);
    }

    [TestMethod]
    public async Task LeaveParty_SoleMemberLeaves_PartyDissolved()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var party = await service.CreatePartyAsync(leader.Id);

        await service.LeavePartyAsync(party.Id, leader.Id);

        var deletedParty = await context.Parties.FindAsync(party.Id);
        Assert.IsNull(deletedParty);

        var members = await context.PartyMembers
            .Where(pm => pm.PartyId == party.Id).ToListAsync();
        Assert.IsEmpty(members);
    }

    [TestMethod]
    public async Task LeaveParty_LeaderLeavesWithMembers_TransfersLeadershipToOldestMember()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var member1 = await SeedUserAsync(context, "member1");
        var member2 = await SeedUserAsync(context, "member2");
        var party = await service.CreatePartyAsync(leader.Id);
        var inv1 = await service.InviteUserAsync(party.Id, leader.Id, member1.Id);
        await service.RespondToUserInviteAsync(inv1.Id, member1.Id, true);
        var inv2 = await service.InviteUserAsync(party.Id, leader.Id, member2.Id);
        await service.RespondToUserInviteAsync(inv2.Id, member2.Id, true);

        await service.LeavePartyAsync(party.Id, leader.Id);

        var updatedParty = await context.Parties.FindAsync(party.Id);
        Assert.IsNotNull(updatedParty);
        Assert.AreEqual(member1.Id, updatedParty.LeaderId);

        var newLeaderMember = await context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == member1.Id);
        Assert.IsNotNull(newLeaderMember);
        Assert.AreEqual(UserRole.Leader, newLeaderMember.Role);

        var oldLeaderGone = await context.PartyMembers
            .AnyAsync(pm => pm.PartyId == party.Id && pm.UserId == leader.Id);
        Assert.IsFalse(oldLeaderGone);
    }
}
