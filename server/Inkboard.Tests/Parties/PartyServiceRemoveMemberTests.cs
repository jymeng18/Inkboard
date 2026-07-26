using Inkboard.Application.Common;
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
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;
        var respondResult = await Service.RespondToUserInviteAsync(invite.Id, member.Id, true);
        Assert.IsTrue(respondResult.IsSuccess);

        var result = await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);
        Assert.IsTrue(result.IsSuccess);

        var isMember = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == member.Id
        );
        Assert.IsFalse(isMember);
    }

    [TestMethod]
    public async Task RemoveMember_PartyNotFound_ReturnsNotFound()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var target = await SeedUserAsync(Context, "target");

        var result = await Service.RemoveMemberAsync(Guid.NewGuid(), leader.Id, target.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Party not found.", result.Error);
    }

    [TestMethod]
    public async Task RemoveMember_NonLeaderCannotKick_ReturnsForbidden()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonLeader = await SeedUserAsync(Context, "nonLeader");
        var target = await SeedUserAsync(Context, "target");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inv1Result = await Service.InviteUserAsync(party.Id, leader.Id, nonLeader.Id);
        Assert.IsTrue(inv1Result.IsSuccess);
        var inv1 = inv1Result.Data!;
        var respond1Result = await Service.RespondToUserInviteAsync(inv1.Id, nonLeader.Id, true);
        Assert.IsTrue(respond1Result.IsSuccess);
        var inv2Result = await Service.InviteUserAsync(party.Id, leader.Id, target.Id);
        Assert.IsTrue(inv2Result.IsSuccess);
        var inv2 = inv2Result.Data!;
        var respond2Result = await Service.RespondToUserInviteAsync(inv2.Id, target.Id, true);
        Assert.IsTrue(respond2Result.IsSuccess);

        var result = await Service.RemoveMemberAsync(party.Id, nonLeader.Id, target.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Only the leader can kick members.", result.Error);
    }

    [TestMethod]
    public async Task RemoveMember_LeaderCannotKickSelf_ReturnsValidationError()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.RemoveMemberAsync(party.Id, leader.Id, leader.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You cannot kick yourself.", result.Error);
    }

    [TestMethod]
    public async Task RemoveMember_TargetNotInParty_ReturnsNotFound()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonMember = await SeedUserAsync(Context, "nonMember");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.RemoveMemberAsync(party.Id, leader.Id, nonMember.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Member not found in party.", result.Error);
    }
}
