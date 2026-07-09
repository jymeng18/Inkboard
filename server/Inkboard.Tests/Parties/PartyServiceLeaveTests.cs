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
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var party = await Service.CreatePartyAsync(leader.Id);
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Id, member.Id, true);

        await Service.LeavePartyAsync(party.Id, member.Id);

        var isMember = await Context.PartyMembers
            .AnyAsync(pm => pm.PartyId == party.Id && pm.UserId == member.Id);
        Assert.IsFalse(isMember);

        var partyExists = await Context.Parties.AnyAsync(p => p.Id == party.Id);
        Assert.IsTrue(partyExists);
    }

    [TestMethod]
    public async Task LeaveParty_PartyNotFound_ThrowsPartyNotFoundException()
    {
        var user = await SeedUserAsync(Context, "user");

        var ex = await AssertThrowsAsync<PartyNotFoundException>(() =>
            Service.LeavePartyAsync(Guid.NewGuid(), user.Id));

        Assert.AreEqual("Party not found.", ex.Message);
    }

    [TestMethod]
    public async Task LeaveParty_NotAMember_ThrowsPartyValidationException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonMember = await SeedUserAsync(Context, "nonMember");
        var party = await Service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.LeavePartyAsync(party.Id, nonMember.Id));

        Assert.AreEqual("You are not in a party.", ex.Message);
    }

    [TestMethod]
    public async Task LeaveParty_SoleMemberLeaves_PartyDissolved()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var party = await Service.CreatePartyAsync(leader.Id);

        await Service.LeavePartyAsync(party.Id, leader.Id);

        var deletedParty = await Context.Parties.FindAsync(party.Id);
        Assert.IsNull(deletedParty);

        var members = await Context.PartyMembers
            .Where(pm => pm.PartyId == party.Id).ToListAsync();
        Assert.IsEmpty(members);
    }

    [TestMethod]
    public async Task LeaveParty_LeaderLeavesWithMembers_TransfersLeadershipToOldestMember()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member1 = await SeedUserAsync(Context, "member1");
        var member2 = await SeedUserAsync(Context, "member2");
        var party = await Service.CreatePartyAsync(leader.Id);
        var inv1 = await Service.InviteUserAsync(party.Id, leader.Id, member1.Id);
        await Service.RespondToUserInviteAsync(inv1.Id, member1.Id, true);
        var inv2 = await Service.InviteUserAsync(party.Id, leader.Id, member2.Id);
        await Service.RespondToUserInviteAsync(inv2.Id, member2.Id, true);

        await Service.LeavePartyAsync(party.Id, leader.Id);

        var updatedParty = await Context.Parties.FindAsync(party.Id);
        Assert.IsNotNull(updatedParty);
        Assert.AreEqual(member1.Id, updatedParty.LeaderId);

        var newLeaderMember = await Context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == member1.Id);
        Assert.IsNotNull(newLeaderMember);
        Assert.AreEqual(UserRole.Leader, newLeaderMember.Role);

        var oldLeaderGone = await Context.PartyMembers
            .AnyAsync(pm => pm.PartyId == party.Id && pm.UserId == leader.Id);
        Assert.IsFalse(oldLeaderGone);
    }
}
