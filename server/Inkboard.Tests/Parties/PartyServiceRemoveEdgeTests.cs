using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceRemoveEdgeTests : PartyTestBase
{
    private async Task<(Party Party, User Leader)> SeedActiveParty()
    {
        var leader = await SeedUserAsync("leader");
        var canvas = await SeedCanvasAsync(leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        return (partyResult.Data!, leader);
    }

    private async Task<User> AddMember(Party party, Guid leaderId, string name)
    {
        var user = await SeedUserAsync(name);
        var inv = await Service.InviteUserAsync(party.Id, leaderId, user.Id);
        await Service.RespondToUserInviteAsync(inv.Data!.Id, user.Id, true);
        return user;
    }

    [TestMethod]
    public async Task RemoveMember_KickedMemberCanBeReInvited()
    {
        var (party, leader) = await SeedActiveParty();
        var member = await AddMember(party, leader.Id, "member");
        await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);

        var reInvite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);

        Assert.IsTrue(reInvite.IsSuccess);
    }

    [TestMethod]
    public async Task RemoveMember_TargetInDifferentParty_ReturnsNotFound()
    {
        var (partyA, leaderA) = await SeedActiveParty();

        var leaderB = await SeedUserAsync("leaderB");
        var canvasB = await SeedCanvasAsync(leaderB.Id, "B");
        var partyBResult = await Service.CreatePartyAsync(leaderB.Id, canvasB.Id);
        var partyB = partyBResult.Data!;
        var memberB = await SeedUserAsync("memberB");
        var inv = await Service.InviteUserAsync(partyB.Id, leaderB.Id, memberB.Id);
        await Service.RespondToUserInviteAsync(inv.Data!.Id, memberB.Id, true);

        // leaderA tries to remove a member that only exists in party B.
        var result = await Service.RemoveMemberAsync(partyA.Id, leaderA.Id, memberB.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Member not found in party.", result.Error);
    }

    [TestMethod]
    public async Task RemoveMember_DoesNotChangeLeadership()
    {
        var (party, leader) = await SeedActiveParty();
        var member = await AddMember(party, leader.Id, "member");

        await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);

        var updated = await Context.Parties.FindAsync(party.Id);
        Assert.AreEqual(leader.Id, updated!.LeaderId);
    }

    [TestMethod]
    public async Task RemoveMember_DecreasesMemberCount()
    {
        var (party, leader) = await SeedActiveParty();
        await AddMember(party, leader.Id, "m1");
        var m2 = await AddMember(party, leader.Id, "m2");

        await Service.RemoveMemberAsync(party.Id, leader.Id, m2.Id);

        var count = await Context.PartyMembers.CountAsync(pm => pm.PartyId == party.Id);
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task RemoveMember_OnlyTargetRemoved_OthersRemain()
    {
        var (party, leader) = await SeedActiveParty();
        var m1 = await AddMember(party, leader.Id, "m1");
        var m2 = await AddMember(party, leader.Id, "m2");

        await Service.RemoveMemberAsync(party.Id, leader.Id, m1.Id);

        var m1Gone = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == m1.Id
        );
        var m2Present = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == m2.Id
        );
        Assert.IsFalse(m1Gone);
        Assert.IsTrue(m2Present);
    }

    [TestMethod]
    public async Task RemoveMember_DownToLeaderOnly_PartyNotDeleted()
    {
        var (party, leader) = await SeedActiveParty();
        var member = await AddMember(party, leader.Id, "member");

        await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);

        var stillExists = await Context.Parties.AnyAsync(p => p.Id == party.Id);
        Assert.IsTrue(stillExists);
    }

    [TestMethod]
    public async Task RemoveMember_KickedMemberCanCreateOwnPartyAfterwards()
    {
        var (party, leader) = await SeedActiveParty();
        var member = await AddMember(party, leader.Id, "member");
        await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);

        var ownCanvas = await SeedCanvasAsync(member.Id, "Member Canvas");
        var result = await Service.CreatePartyAsync(member.Id, ownCanvas.Id);

        Assert.IsTrue(result.IsSuccess);
    }
}
