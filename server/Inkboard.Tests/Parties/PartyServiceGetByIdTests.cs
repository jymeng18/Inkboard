using Inkboard.Application.Common;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceGetByIdTests : PartyTestBase
{
    [TestMethod]
    public async Task GetPartyById_PartyNotFound_ReturnsNotFound()
    {
        var result = await Service.GetPartyByIdAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Party not found.", result.Error);
    }

    [TestMethod]
    public async Task GetPartyById_ReturnsLeaderAndCanvasIds()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(party.Id, result.Data!.Id);
        Assert.AreEqual(leader.Id, result.Data.LeaderId);
        Assert.AreEqual(canvas.Id, result.Data.CanvasId);
    }

    [TestMethod]
    public async Task GetPartyById_SoloParty_ReturnsSingleLeaderMember()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Data!.Members.Count);
        Assert.AreEqual(leader.Id, result.Data.Members[0].UserId);
        Assert.AreEqual("Leader", result.Data.Members[0].Role);
    }

    [TestMethod]
    public async Task GetPartyById_RoleSerializedAsString()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        Assert.IsTrue(inviteResult.IsSuccess);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, member.Id, true);

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        var roles = result.Data!.Members.ConvertAll(m => m.Role);
        CollectionAssert.Contains(roles, "Leader");
        CollectionAssert.Contains(roles, "Member");
    }

    [TestMethod]
    public async Task GetPartyById_MemberCountGrowsAfterAccept()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        for (int i = 0; i < 3; i++)
        {
            var m = await SeedUserAsync(Context, $"member{i}");
            var inv = await Service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await Service.RespondToUserInviteAsync(inv.Data!.Id, m.Id, true);
        }

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(4, result.Data!.Members.Count);
    }

    [TestMethod]
    public async Task GetPartyById_DeclinedInviteeNotIncludedInMembers()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var invited = await SeedUserAsync(Context, "invited");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, invited.Id, false);

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Data!.Members.Count);
        Assert.IsFalse(result.Data.Members.Exists(m => m.UserId == invited.Id));
    }

    [TestMethod]
    public async Task GetPartyById_LeftMemberNotIncluded()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, member.Id, true);
        await Service.LeavePartyAsync(party.Id, member.Id);

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.Data!.Members.Count);
        Assert.IsFalse(result.Data.Members.Exists(m => m.UserId == member.Id));
    }

    [TestMethod]
    public async Task GetPartyById_AfterLeadershipTransfer_ReflectsNewLeaderRole()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, member.Id, true);
        await Service.LeavePartyAsync(party.Id, leader.Id);

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(member.Id, result.Data!.LeaderId);
        var newLeader = result.Data.Members.Find(m => m.UserId == member.Id);
        Assert.IsNotNull(newLeader);
        Assert.AreEqual("Leader", newLeader.Role);
    }

    [TestMethod]
    public async Task GetPartyById_CanvasLinkSeveredAfterForceEnd_ReturnsNullCanvasId()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var member = await SeedUserAsync(Context, "member");
        var (party, _) = await SeedPartyAsync(Context, leader.Id, "Shared Canvas");
        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await Service.RespondToUserInviteAsync(inviteResult.Data!.Id, member.Id, true);
        await Service.LeavePartyAsync(party.Id, leader.Id);

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Data!.CanvasId);
    }

    [TestMethod]
    public async Task GetPartyById_MembersHaveJoinedAtTimestamps()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var result = await Service.GetPartyByIdAsync(party.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreNotEqual(default(DateTime), result.Data!.Members[0].JoinedAt);
    }
}
