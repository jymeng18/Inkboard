using Inkboard.Application.Common;
using Inkboard.Domain.Models;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceInviteTests : PartyTestBase
{
    [TestMethod]
    public async Task InviteUser_LeaderInvitesUser_CreatesPendingInvite()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(result.IsSuccess);
        var invite = result.Data!;

        Assert.AreEqual(party.Id, invite.PartyId);
        Assert.AreEqual(leader.Id, invite.InvitedByUserId);
        Assert.AreEqual(invited.Id, invite.InvitedUserId);
        Assert.AreEqual(InviteStatus.Pending, invite.InviteStatus);
    }

    [TestMethod]
    public async Task InviteUser_InviteHasFiveMinuteExpiryWindow()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(result.IsSuccess);
        var invite = result.Data!;

        var expectedExpiry = DateTime.UtcNow.AddMinutes(5);
        Assert.IsTrue(invite.ExpiresAt <= expectedExpiry.AddSeconds(1));
        Assert.IsTrue(invite.ExpiresAt >= expectedExpiry.AddSeconds(-1));
    }

    [TestMethod]
    public async Task InviteUser_PartyNotFound_ReturnsNotFound()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");

        var result = await Service.InviteUserAsync(Guid.NewGuid(), leader.Id, invited.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Party not found.", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_NonLeaderCannotInviteAnotherUser_ReturnsForbidden()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonLeader = await SeedUserAsync(Context, "nonLeader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.InviteUserAsync(party.Id, nonLeader.Id, invited.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Only the leader can invite people.", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_NonLeaderAttemptsToInviteLeader_ReturnsForbidden()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var nonLeader = await SeedUserAsync(Context, "nonLeader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.InviteUserAsync(party.Id, nonLeader.Id, leader.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Only the leader can invite people.", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_CannotInviteSelf_ReturnsValidationError()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.InviteUserAsync(party.Id, leader.Id, leader.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You cannot invite yourself.", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_CannotInviteExistingMember_ReturnsConflict()
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

        var result = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("User is already in the party.", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_BlockedUserCannotBeInvited_ReturnsValidationError()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var blockResult = await Service.BlockUserAsync(leader.Id, invited.Id);
        Assert.IsTrue(blockResult.IsSuccess);

        var result = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You have blocked this user.", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_PartyFullAtFiveMembers_ReturnsValidationError()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync(Context, $"member{i}");
            var invResult = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            Assert.IsTrue(invResult.IsSuccess);
            var inv = invResult.Data!;
            var respondResult = await Service.RespondToUserInviteAsync(inv.Id, m.Id, true);
            Assert.IsTrue(respondResult.IsSuccess);
        }

        var extra = await SeedUserAsync(Context, "extra");
        var result = await Service.InviteUserAsync(party.Id, leader.Id, extra.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("Party is full. (Max 5 members)", result.Error);
    }

    [TestMethod]
    public async Task InviteUser_PendingInviteAlreadyExists_ReturnsConflict()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var firstResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsTrue(firstResult.IsSuccess);

        var result = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("An invite is already pending for this user.", result.Error);
    }
}
