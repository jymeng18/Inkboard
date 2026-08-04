using Inkboard.Application.Common;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceCanvasTests : PartyTestBase
{
    private CanvasService CreateCanvasService()
    {
        return new CanvasService(new CanvasRepository(Context), new PartyRepository(Context));
    }

    [TestMethod]
    public async Task LeaveParty_LeaderLeavesWithActiveCanvas_ForceEndsSession_CanvasNotDeleted_LinkSevered()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var member2 = await SeedUserAsync(Context, "member2");
        var (party, canvas) = await SeedPartyAsync(Context, leader.Id, "Shared Canvas");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;
        var respondResult = await Service.RespondToUserInviteAsync(invite.Id, member.Id, true);
        Assert.IsTrue(respondResult.IsSuccess);

        // Three members, so the leader leaving hands off leadership and keeps the
        // party alive rather than dissolving it down to one person.
        var invite2 = await Service.InviteUserAsync(party.Id, leader.Id, member2.Id);
        await Service.RespondToUserInviteAsync(invite2.Data!.Id, member2.Id, true);

        var result = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

        var updatedParty = await Context.Parties.FindAsync(party.Id);
        Assert.IsNotNull(updatedParty);
        Assert.AreEqual(member.Id, updatedParty.LeaderId);
        Assert.IsNull(updatedParty.CanvasId);

        var existingCanvas = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(existingCanvas, "Canvas must NOT be deleted when session is force-ended.");

        var oldLeaderGone = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == leader.Id
        );
        Assert.IsFalse(oldLeaderGone);
    }

    [TestMethod]
    public async Task LeaveParty_LeaderLeavesWithActiveCanvas_CanvasOwnershipDoesNotChange()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var (party, canvas) = await SeedPartyAsync(Context, leader.Id, "Owned Canvas");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;
        var respondResult = await Service.RespondToUserInviteAsync(invite.Id, member.Id, true);
        Assert.IsTrue(respondResult.IsSuccess);

        var result = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

        var existingCanvas = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(existingCanvas);
        Assert.AreEqual(leader.Id, existingCanvas.OwnerId, "Canvas ownership must never transfer.");
    }

    [TestMethod]
    public async Task ForceEndSession_ClearsPartyCanvasLink_CanvasSurvives()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var (party, canvas) = await SeedPartyAsync(Context, leader.Id, "Test Canvas");
        Assert.IsNotNull(party.CanvasId);

        var canvasService = CreateCanvasService();
        var result = await canvasService.ForceEndSessionAsync(canvas.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

        var updatedParty = await Context.Parties.FindAsync(party.Id);
        Assert.IsNull(updatedParty!.CanvasId);

        var existingCanvas = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(existingCanvas, "Canvas must NOT be deleted by ForceEndSession.");
    }

    [TestMethod]
    public async Task ForceEndSession_PartyDoesNotOwnCanvas_ReturnsForbidden()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var (party, canvas) = await SeedPartyAsync(Context, leader.Id, "Owned Canvas");

        var otherUser = await SeedUserAsync(Context, "other");
        var otherCanvas = await SeedCanvasAsync(Context, otherUser.Id, "Other Canvas");
        var partyRepo = new PartyRepository(Context);
        var otherParty = new Party
        {
            LeaderId = otherUser.Id,
            CanvasId = otherCanvas.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await partyRepo.CreatePartyAsync(otherParty);

        var canvasService = CreateCanvasService();
        var result = await canvasService.ForceEndSessionAsync(otherCanvas.Id, leader.Id);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Canvas does not belong to Party.", result.Error);
    }

    [TestMethod]
    public async Task CreateParty_WithCanvasId_StoresCanvasLink()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);

        var result = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(result.IsSuccess);
        var party = result.Data!;

        Assert.AreEqual(canvas.Id, party.CanvasId);

        var savedParty = await Context.Parties.FindAsync(party.Id);
        Assert.IsNotNull(savedParty);
        Assert.AreEqual(canvas.Id, savedParty.CanvasId);
    }

    [TestMethod]
    public async Task CreateCanvas_DoesNotCreateParty()
    {
        var user = await SeedUserAsync(Context, "user");

        var canvasService = CreateCanvasService();
        var result = await canvasService.CreateCanvasAsync(user.Id, "My Canvas");
        Assert.IsTrue(result.IsSuccess);
        var canvas = result.Data!;

        Assert.AreEqual(user.Id, canvas.OwnerId);

        var partiesForUser = await Context.Parties.AnyAsync(p => p.LeaderId == user.Id);
        Assert.IsFalse(partiesForUser, "Creating a canvas must NOT create a party.");
    }

    [TestMethod]
    public async Task FullWorkflow_CreateCanvasThenCreatePartyThenInvite_LeadershipTransferForceEndsSession()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var member2 = await SeedUserAsync(Context, "member2");

        var canvasService = CreateCanvasService();
        var canvasResult = await canvasService.CreateCanvasAsync(leader.Id, "Workflow Canvas");
        Assert.IsTrue(canvasResult.IsSuccess);
        var canvas = canvasResult.Data!;

        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        Assert.AreEqual(canvas.Id, party.CanvasId);

        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        var invite = inviteResult.Data!;
        var respondResult = await Service.RespondToUserInviteAsync(invite.Id, member.Id, true);
        Assert.IsTrue(respondResult.IsSuccess);

        // Three members, so leadership transfers on leave and the party survives.
        var invite2 = await Service.InviteUserAsync(party.Id, leader.Id, member2.Id);
        await Service.RespondToUserInviteAsync(invite2.Data!.Id, member2.Id, true);

        var leaveResult = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(leaveResult.IsSuccess);

        var updatedParty = await Context.Parties.FindAsync(party.Id);
        Assert.IsNotNull(updatedParty);
        Assert.AreEqual(member.Id, updatedParty.LeaderId);
        Assert.IsNull(updatedParty.CanvasId);

        var existingCanvas = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(existingCanvas, "Canvas must survive force-end.");
        Assert.AreEqual(leader.Id, existingCanvas.OwnerId, "Canvas ownership never changes.");
    }
}
