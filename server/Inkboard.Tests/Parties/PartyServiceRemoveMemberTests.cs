using Inkboard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceRemoveMemberTests : PartyTestBase
{
    [TestMethod]
    public async Task RemoveMember_LeaderKicksMember_MemberRemoved()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var party = await Service.CreatePartyAsync(leader.Id);
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Id, member.Id, true);

        await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);

        var isMember = await Context.PartyMembers
            .AnyAsync(pm => pm.PartyId == party.Id && pm.UserId == member.Id);
        Assert.IsFalse(isMember);
    }

    [TestMethod]
    public async Task RemoveMember_PartyNotFound_ThrowsPartyNotFoundException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var target = await SeedUserAsync(Context, "target");

        var ex = await AssertThrowsAsync<PartyNotFoundException>(() =>
            Service.RemoveMemberAsync(Guid.NewGuid(), leader.Id, target.Id));

        Assert.AreEqual("Party not found.", ex.Message);
    }

    [TestMethod]
    public async Task RemoveMember_NonLeaderCannotKick_ThrowsPartyForbiddenException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonLeader = await SeedUserAsync(Context, "nonLeader");
        var target = await SeedUserAsync(Context, "target");
        var party = await Service.CreatePartyAsync(leader.Id);
        var inv1 = await Service.InviteUserAsync(party.Id, leader.Id, nonLeader.Id);
        await Service.RespondToUserInviteAsync(inv1.Id, nonLeader.Id, true);
        var inv2 = await Service.InviteUserAsync(party.Id, leader.Id, target.Id);
        await Service.RespondToUserInviteAsync(inv2.Id, target.Id, true);

        var ex = await AssertThrowsAsync<PartyForbiddenException>(() =>
            Service.RemoveMemberAsync(party.Id, nonLeader.Id, target.Id));

        Assert.AreEqual("Only leader can kick members.", ex.Message);
    }

    [TestMethod]
    public async Task RemoveMember_LeaderCannotKickSelf_ThrowsPartyValidationException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var party = await Service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.RemoveMemberAsync(party.Id, leader.Id, leader.Id));

        Assert.AreEqual("You cannot kick yourself.", ex.Message);
    }

    [TestMethod]
    public async Task RemoveMember_TargetNotInParty_ThrowsPartyValidationException()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonMember = await SeedUserAsync(Context, "nonMember");
        var party = await Service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.RemoveMemberAsync(party.Id, leader.Id, nonMember.Id));

        Assert.AreEqual("Member not in party.", ex.Message);
    }
}
