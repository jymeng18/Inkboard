using Inkboard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceRemoveMemberTests : PartyTestBase
{
    [TestMethod]
    public async Task RemoveMember_LeaderKicksMember_MemberRemoved()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var member = await SeedUserAsync(context, "member");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await service.RespondToUserInviteAsync(invite.Id, member.Id, true);

        await service.RemoveMemberAsync(party.Id, leader.Id, member.Id);

        var isMember = await context.PartyMembers
            .AnyAsync(pm => pm.PartyId == party.Id && pm.UserId == member.Id);
        Assert.IsFalse(isMember);
    }

    [TestMethod]
    public async Task RemoveMember_PartyNotFound_ThrowsPartyNotFoundException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var target = await SeedUserAsync(context, "target");

        var ex = await AssertThrowsAsync<PartyNotFoundException>(() =>
            service.RemoveMemberAsync(Guid.NewGuid(), leader.Id, target.Id));

        Assert.AreEqual("Party not found.", ex.Message);
    }

    [TestMethod]
    public async Task RemoveMember_NonLeaderCannotKick_ThrowsPartyForbiddenException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var nonLeader = await SeedUserAsync(context, "nonLeader");
        var target = await SeedUserAsync(context, "target");
        var party = await service.CreatePartyAsync(leader.Id);
        var inv1 = await service.InviteUserAsync(party.Id, leader.Id, nonLeader.Id);
        await service.RespondToUserInviteAsync(inv1.Id, nonLeader.Id, true);
        var inv2 = await service.InviteUserAsync(party.Id, leader.Id, target.Id);
        await service.RespondToUserInviteAsync(inv2.Id, target.Id, true);

        var ex = await AssertThrowsAsync<PartyForbiddenException>(() =>
            service.RemoveMemberAsync(party.Id, nonLeader.Id, target.Id));

        Assert.AreEqual("Only leader can kick members.", ex.Message);
    }

    [TestMethod]
    public async Task RemoveMember_LeaderCannotKickSelf_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var party = await service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.RemoveMemberAsync(party.Id, leader.Id, leader.Id));

        Assert.AreEqual("You cannot kick yourself.", ex.Message);
    }

    [TestMethod]
    public async Task RemoveMember_TargetNotInParty_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var nonMember = await SeedUserAsync(context, "nonMember");
        var party = await service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.RemoveMemberAsync(party.Id, leader.Id, nonMember.Id));

        Assert.AreEqual("Member not in party.", ex.Message);
    }
}
