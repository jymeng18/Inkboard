using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Inkboard.Tests.Parties;

/// <summary>
/// The party↔canvas link rules: re-pointing a party at a canvas (leader-only,
/// canvas must exist, members are pulled in), ending a session (leader-only,
/// dissolves the party), and reading a user's own pending invites.
/// </summary>
[TestClass]
public sealed class PartySessionLinkTests : PartyTestBase
{
    private async Task<PartyMember> AddMemberAsync(Guid partyId, Guid userId)
    {
        var member = new PartyMember
        {
            PartyId = partyId,
            UserId = userId,
            Role = UserRole.Member,
        };
        Context.PartyMembers.Add(member);
        await Context.SaveChangesAsync();
        return member;
    }

    private async Task<PartyInvite> SeedInviteAsync(
        Guid partyId,
        Guid invitedByUserId,
        Guid invitedUserId,
        InviteStatus status = InviteStatus.Pending
    )
    {
        var invite = new PartyInvite
        {
            PartyId = partyId,
            InvitedByUserId = invitedByUserId,
            InvitedUserId = invitedUserId,
            InviteStatus = status,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
        };
        Context.PartyInvites.Add(invite);
        await Context.SaveChangesAsync();
        return invite;
    }

    // ─── SetPartyCanvasAsync ────────────────────────────────────

    [TestMethod]
    public async Task SetPartyCanvas_PartyNotFound_ReturnsNotFound()
    {
        var leader = await SeedUserAsync("leader");
        var canvas = await SeedCanvasAsync(leader.Id);

        var result = await Service.SetPartyCanvasAsync(Guid.NewGuid(), leader.Id, canvas.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task SetPartyCanvas_NonLeaderCaller_ReturnsForbidden()
    {
        var leader = await SeedUserAsync("leader");
        var member = await SeedUserAsync("member");
        var (party, _) = await SeedPartyAsync(leader.Id);
        await AddMemberAsync(party.Id, member.Id);
        var otherCanvas = await SeedCanvasAsync(leader.Id, "Other");

        var result = await Service.SetPartyCanvasAsync(party.Id, member.Id, otherCanvas.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task SetPartyCanvas_CanvasDoesNotExist_ReturnsNotFound()
    {
        var leader = await SeedUserAsync("leader");
        var (party, _) = await SeedPartyAsync(leader.Id);

        var result = await Service.SetPartyCanvasAsync(party.Id, leader.Id, Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Canvas does not exist.", result.Error);
    }

    [TestMethod]
    public async Task SetPartyCanvas_LeaderRelinks_UpdatesCanvasId()
    {
        var leader = await SeedUserAsync("leader");
        var (party, firstCanvas) = await SeedPartyAsync(leader.Id);
        var secondCanvas = await SeedCanvasAsync(leader.Id, "Second");

        var result = await Service.SetPartyCanvasAsync(party.Id, leader.Id, secondCanvas.Id);

        Assert.IsTrue(result.IsSuccess);
        var reloaded = await Context.Parties.AsNoTracking().SingleAsync(p => p.Id == party.Id);
        Assert.AreEqual(secondCanvas.Id, reloaded.CanvasId);
        Assert.AreNotEqual(firstCanvas.Id, reloaded.CanvasId);
    }

    [TestMethod]
    public async Task SetPartyCanvas_WithOtherMembers_NotifiesEveryoneButLeader()
    {
        var leader = await SeedUserAsync("leader");
        var member = await SeedUserAsync("member");
        var (party, _) = await SeedPartyAsync(leader.Id);
        await AddMemberAsync(party.Id, member.Id);
        var target = await SeedCanvasAsync(leader.Id, "Target");

        var result = await Service.SetPartyCanvasAsync(party.Id, leader.Id, target.Id);

        Assert.IsTrue(result.IsSuccess);
        Notifier.Verify(
            n => n.NotifyPartyCanvasOpened(
                party.Id,
                target.Id,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(member.Id) && !ids.Contains(leader.Id))
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SetPartyCanvas_SoloLeader_SendsNoCanvasOpenedNotification()
    {
        var leader = await SeedUserAsync("leader");
        var (party, _) = await SeedPartyAsync(leader.Id);
        var target = await SeedCanvasAsync(leader.Id, "Target");

        var result = await Service.SetPartyCanvasAsync(party.Id, leader.Id, target.Id);

        Assert.IsTrue(result.IsSuccess);
        Notifier.Verify(
            n => n.NotifyPartyCanvasOpened(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<Guid>>()
            ),
            Times.Never
        );
    }

    // ─── EndSessionAsync ────────────────────────────────────────

    [TestMethod]
    public async Task EndSession_PartyNotFound_ReturnsNotFound()
    {
        var leader = await SeedUserAsync("leader");

        var result = await Service.EndSessionAsync(Guid.NewGuid(), leader.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task EndSession_NonLeaderCaller_ReturnsForbidden()
    {
        var leader = await SeedUserAsync("leader");
        var member = await SeedUserAsync("member");
        var (party, _) = await SeedPartyAsync(leader.Id);
        await AddMemberAsync(party.Id, member.Id);

        var result = await Service.EndSessionAsync(party.Id, member.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        // The party must survive a forbidden end attempt.
        Assert.IsTrue(await Context.Parties.AnyAsync(p => p.Id == party.Id));
    }

    [TestMethod]
    public async Task EndSession_LeaderEnds_DeletesPartyAndAllMemberships()
    {
        var leader = await SeedUserAsync("leader");
        var member = await SeedUserAsync("member");
        var (party, _) = await SeedPartyAsync(leader.Id);
        await AddMemberAsync(party.Id, member.Id);

        var result = await Service.EndSessionAsync(party.Id, leader.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(await Context.Parties.AnyAsync(p => p.Id == party.Id));
        Assert.IsFalse(await Context.PartyMembers.AnyAsync(pm => pm.PartyId == party.Id));
    }

    [TestMethod]
    public async Task EndSession_LeaderEnds_NotifiesAllMembersPartyEnded()
    {
        var leader = await SeedUserAsync("leader");
        var member = await SeedUserAsync("member");
        var (party, _) = await SeedPartyAsync(leader.Id);
        await AddMemberAsync(party.Id, member.Id);

        var result = await Service.EndSessionAsync(party.Id, leader.Id);

        Assert.IsTrue(result.IsSuccess);
        Notifier.Verify(
            n => n.NotifyPartyEnded(
                party.Id,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(leader.Id) && ids.Contains(member.Id))
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task EndSession_LeaderEnds_CanvasItselfIsNotDeleted()
    {
        var leader = await SeedUserAsync("leader");
        var (party, canvas) = await SeedPartyAsync(leader.Id);

        var result = await Service.EndSessionAsync(party.Id, leader.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(await Context.Canvas.AnyAsync(c => c.Id == canvas.Id));
    }

    // ─── GetPartyInvitesByUserIdAsync ───────────────────────────

    [TestMethod]
    public async Task GetPartyInvites_ReturnsOnlyThisUsersPendingInvites()
    {
        var leader = await SeedUserAsync("leader");
        var invitee = await SeedUserAsync("invitee");
        var (party, _) = await SeedPartyAsync(leader.Id);
        var pending = await SeedInviteAsync(party.Id, leader.Id, invitee.Id);

        var result = await Service.GetPartyInvitesByUserIdAsync(invitee.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Data!);
        Assert.AreEqual(pending.Id, result.Data![0].Id);
    }

    [TestMethod]
    public async Task GetPartyInvites_ExcludesAnsweredInvites()
    {
        var leader = await SeedUserAsync("leader");
        var invitee = await SeedUserAsync("invitee");
        var (party, _) = await SeedPartyAsync(leader.Id);
        await SeedInviteAsync(party.Id, leader.Id, invitee.Id, InviteStatus.Declined);
        await SeedInviteAsync(party.Id, leader.Id, invitee.Id, InviteStatus.Accepted);

        var result = await Service.GetPartyInvitesByUserIdAsync(invitee.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Data!);
    }

    [TestMethod]
    public async Task GetPartyInvites_DoesNotLeakOtherUsersInvites()
    {
        var leader = await SeedUserAsync("leader");
        var invitee = await SeedUserAsync("invitee");
        var someoneElse = await SeedUserAsync("someoneElse");
        var (party, _) = await SeedPartyAsync(leader.Id);
        await SeedInviteAsync(party.Id, leader.Id, someoneElse.Id);

        var result = await Service.GetPartyInvitesByUserIdAsync(invitee.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Data!);
    }
}
