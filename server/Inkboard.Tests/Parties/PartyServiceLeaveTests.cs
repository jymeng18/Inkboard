using Inkboard.Application.Common;
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
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;
        var respondResult = await Service.RespondToUserInviteAsync(invite.Id, member.Id, true);
        Assert.IsTrue(respondResult.IsSuccess);

        var result = await Service.LeavePartyAsync(party.Id, member.Id);
        Assert.IsTrue(result.IsSuccess);

        var isMember = await Context.PartyMembers
            .AnyAsync(pm => pm.PartyId == party.Id && pm.UserId == member.Id);
        Assert.IsFalse(isMember);

        var partyExists = await Context.Parties.AnyAsync(p => p.Id == party.Id);
        Assert.IsTrue(partyExists);
    }

    [TestMethod]
    public async Task LeaveParty_PartyNotFound_ReturnsNotFound()
    {
        var user = await SeedUserAsync(Context, "user");

        var result = await Service.LeavePartyAsync(Guid.NewGuid(), user.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Party not found.", result.Error);
    }

    [TestMethod]
    public async Task LeaveParty_NotAMember_ReturnsValidationError()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonMember = await SeedUserAsync(Context, "nonMember");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.LeavePartyAsync(party.Id, nonMember.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You are not in this party.", result.Error);
    }

    [TestMethod]
    public async Task LeaveParty_SoleMemberLeaves_PartyDissolved()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

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
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inv1Result = await Service.InviteUserAsync(party.Id, leader.Id, member1.Id);
        Assert.IsTrue(inv1Result.IsSuccess);
        var inv1 = inv1Result.Data!;
        var respond1Result = await Service.RespondToUserInviteAsync(inv1.Id, member1.Id, true);
        Assert.IsTrue(respond1Result.IsSuccess);
        var inv2Result = await Service.InviteUserAsync(party.Id, leader.Id, member2.Id);
        Assert.IsTrue(inv2Result.IsSuccess);
        var inv2 = inv2Result.Data!;
        var respond2Result = await Service.RespondToUserInviteAsync(inv2.Id, member2.Id, true);
        Assert.IsTrue(respond2Result.IsSuccess);

        var result = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

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
