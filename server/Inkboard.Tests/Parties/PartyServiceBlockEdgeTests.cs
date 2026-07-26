using Inkboard.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceBlockEdgeTests : PartyTestBase
{
    [TestMethod]
    public async Task BlockUser_IsDirectional_ReverseInviteStillAllowed()
    {
        // A blocks B. That must not stop B (as leader) from inviting A.
        var a = await SeedUserAsync(Context, "a");
        var b = await SeedUserAsync(Context, "b");
        var canvas = await SeedCanvasAsync(Context, b.Id);
        var partyResult = await Service.CreatePartyAsync(b.Id, canvas.Id);
        var party = partyResult.Data!;

        await Service.BlockUserAsync(a.Id, b.Id);

        var result = await Service.InviteUserAsync(party.Id, b.Id, a.Id);
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task BlockUser_BlockingCurrentMember_DoesNotRemoveThemFromParty()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var inv = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(inv.Data!.Id, member.Id, true);

        var blockResult = await Service.BlockUserAsync(leader.Id, member.Id);
        Assert.IsTrue(blockResult.IsSuccess);

        var stillMember = await Context.PartyMembers.AnyAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == member.Id
        );
        Assert.IsTrue(stillMember, "Blocking a user does not eject them from the party.");
    }

    [TestMethod]
    public async Task BlockUser_TwoUsersCanBlockEachOtherIndependently()
    {
        var a = await SeedUserAsync(Context, "a");
        var b = await SeedUserAsync(Context, "b");

        var first = await Service.BlockUserAsync(a.Id, b.Id);
        var second = await Service.BlockUserAsync(b.Id, a.Id);

        Assert.IsTrue(first.IsSuccess);
        Assert.IsTrue(second.IsSuccess);
        var count = await Context.BlockLists.CountAsync();
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task BlockUser_SameUserBlocksMultipleTargets_AllRecorded()
    {
        var user = await SeedUserAsync(Context, "user");
        var t1 = await SeedUserAsync(Context, "t1");
        var t2 = await SeedUserAsync(Context, "t2");

        await Service.BlockUserAsync(user.Id, t1.Id);
        await Service.BlockUserAsync(user.Id, t2.Id);

        var count = await Context.BlockLists.CountAsync(bl => bl.UserId == user.Id);
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task BlockUser_RecordsCreatedAtTimestamp()
    {
        var user = await SeedUserAsync(Context, "user");
        var target = await SeedUserAsync(Context, "target");
        var before = DateTime.UtcNow.AddSeconds(-1);

        await Service.BlockUserAsync(user.Id, target.Id);

        var block = await Context.BlockLists.FirstAsync(bl =>
            bl.UserId == user.Id && bl.BlockedUserId == target.Id
        );
        Assert.IsTrue(block.CreatedAt >= before);
        Assert.IsTrue(block.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [TestMethod]
    public async Task BlockUser_BlockedThenReInviteAttemptFails()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        var party = partyResult.Data!;
        var inv = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(inv.Data!.Id, member.Id, true);
        await Service.RemoveMemberAsync(party.Id, leader.Id, member.Id);
        await Service.BlockUserAsync(leader.Id, member.Id);

        var result = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You have blocked this user.", result.Error);
    }
}
