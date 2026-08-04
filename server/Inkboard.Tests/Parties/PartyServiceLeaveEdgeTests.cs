using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceLeaveEdgeTests : PartyTestBase
{
    [TestMethod]
    public async Task LeaveParty_LeaderLeavesTwoMemberParty_PartyDissolved()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Data!.Id, member.Id, true);

        var result = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

        // Only one person would remain, so the whole party dissolves instead of
        // leaving a lone member behind.
        var deleted = await Context.Parties.FindAsync(party.Id);
        Assert.IsNull(deleted);
        var anyMembers = await Context.PartyMembers.AnyAsync(pm => pm.PartyId == party.Id);
        Assert.IsFalse(anyMembers);
    }

    [TestMethod]
    public async Task LeaveParty_LeadershipGoesToOldestByJoinedAt_NotInsertionOrder()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var lateJoiner = await SeedUserAsync(Context, "late");
        var earlyJoiner = await SeedUserAsync(Context, "early");
        var (party, _) = await SeedPartyAsync(Context, leader.Id);
        var repo = new PartyRepository(Context);

        // Insert the "late" member first, but with a newer JoinedAt.
        await repo.AddMemberAsync(
            new PartyMember
            {
                PartyId = party.Id,
                UserId = lateJoiner.Id,
                Role = UserRole.Member,
                JoinedAt = DateTimeOffset.UtcNow,
            }
        );
        // Insert the "early" member second, but with an older JoinedAt.
        await repo.AddMemberAsync(
            new PartyMember
            {
                PartyId = party.Id,
                UserId = earlyJoiner.Id,
                Role = UserRole.Member,
                JoinedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            }
        );

        var result = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

        var updated = await Context.Parties.FindAsync(party.Id);
        Assert.AreEqual(earlyJoiner.Id, updated!.LeaderId);
    }

    [TestMethod]
    public async Task LeaveParty_NonLeaderLeavesTwoMemberParty_PartyNotDissolved()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Data!.Id, member.Id, true);

        var result = await Service.LeavePartyAsync(party.Id, member.Id);
        Assert.IsTrue(result.IsSuccess);

        var deleted = await Context.Parties.FindAsync(party.Id);
        Assert.IsNotNull(deleted);
    }

    [TestMethod]
    public async Task LeaveParty_OldLeaderCanCreateNewPartyAfterTransfer()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Data!.Id, member.Id, true);
        await Service.LeavePartyAsync(party.Id, leader.Id);

        var newCanvas = await SeedCanvasAsync(Context, leader.Id, "New");
        var result = await Service.CreatePartyAsync(leader.Id, newCanvas.Id);

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task LeaveParty_MemberLeavesTwoMemberParty_DoesntDissolve()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var invite = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(invite.Data!.Id, member.Id, true);

        var result = await Service.LeavePartyAsync(party.Id, member.Id);
        Assert.IsTrue(result.IsSuccess);

        var deleted = await Context.Parties.FindAsync(party.Id);
        Assert.IsNotNull(deleted);

        var secondLeave = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(secondLeave.IsSuccess);
    }

    [TestMethod]
    public async Task LeaveParty_SoleLeaderLeaves_RemovesMembershipRow()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;

        await Service.LeavePartyAsync(party.Id, leader.Id);

        var anyMembers = await Context.PartyMembers.AnyAsync(pm => pm.PartyId == party.Id);
        Assert.IsFalse(anyMembers);
    }

    [TestMethod]
    public async Task LeaveParty_AfterTransfer_ExactlyOneLeaderRemains()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member1 = await SeedUserAsync(Context, "member1");
        var member2 = await SeedUserAsync(Context, "member2");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        foreach (var m in new[] { member1, member2 })
        {
            var inv = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await Service.RespondToUserInviteAsync(inv.Data!.Id, m.Id, true);
        }

        await Service.LeavePartyAsync(party.Id, leader.Id);

        var leaders = await Context.PartyMembers.CountAsync(pm =>
            pm.PartyId == party.Id && pm.Role == UserRole.Leader
        );
        Assert.AreEqual(1, leaders);
    }

    [TestMethod]
    public async Task LeaveParty_LeaderLeavesFullParty_TransfersAndDropsToFour()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync(Context, $"member{i}");
            var inv = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await Service.RespondToUserInviteAsync(inv.Data!.Id, m.Id, true);
        }

        var result = await Service.LeavePartyAsync(party.Id, leader.Id);
        Assert.IsTrue(result.IsSuccess);

        var count = await Context.PartyMembers.CountAsync(pm => pm.PartyId == party.Id);
        Assert.AreEqual(4, count);
        var oldLeaderGone = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == leader.Id
        );
        Assert.IsFalse(oldLeaderGone);
    }
}
