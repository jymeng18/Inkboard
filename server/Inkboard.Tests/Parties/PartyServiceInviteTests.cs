using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceInviteTests : PartyTestBase
{
    [TestMethod]
    public async Task InviteUser_LeaderInvitesUser_CreatesPendingInvite()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);

        var invite = await service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        Assert.IsNotNull(invite);
        Assert.AreEqual(party.Id, invite.PartyId);
        Assert.AreEqual(leader.Id, invite.InvitedByUserId);
        Assert.AreEqual(invited.Id, invite.InvitedUserId);
        Assert.AreEqual(InviteStatus.Pending, invite.InviteStatus);
    }

    [TestMethod]
    public async Task InviteUser_InviteHasFiveMinuteExpiryWindow()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);

        var invite = await service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(5);
        Assert.IsTrue(invite.ExpiresAt <= expectedExpiry.AddSeconds(1));
        Assert.IsTrue(invite.ExpiresAt >= expectedExpiry.AddSeconds(-1));
    }

    [TestMethod]
    public async Task InviteUser_PartyNotFound_ThrowsPartyNotFoundException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");

        var ex = await AssertThrowsAsync<PartyNotFoundException>(() =>
            service.InviteUserAsync(Guid.NewGuid(), leader.Id, invited.Id));

        Assert.AreEqual("Party not found.", ex.Message);
    }

    [TestMethod]
    public async Task InviteUser_NonLeaderCannotInviteAnotherUser_ThrowsPartyForbiddenException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var nonLeader = await SeedUserAsync(context, "nonLeader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyForbiddenException>(() =>
            service.InviteUserAsync(party.Id, nonLeader.Id, invited.Id));

        Assert.AreEqual("Only leader can invite people.", ex.Message);
    }

    [TestMethod]
    public async Task InviteUser_NonLeaderAttemptsToInviteLeader_ThrowsPartyForbiddenException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var nonLeader = await SeedUserAsync(context, "nonLeader");
        var party = await service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyForbiddenException>(() =>
            service.InviteUserAsync(party.Id, nonLeader.Id, leader.Id));

        Assert.AreEqual("Only leader can invite people.", ex.Message);
    }

    [TestMethod]
    public async Task InviteUser_CannotInviteSelf_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var party = await service.CreatePartyAsync(leader.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.InviteUserAsync(party.Id, leader.Id, leader.Id));

        Assert.AreEqual("You cannot invite yourself.", ex.Message);
    }

    [TestMethod]
    public async Task InviteUser_CannotInviteExistingMember_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var member = await SeedUserAsync(context, "member");
        var party = await service.CreatePartyAsync(leader.Id);
        var invite = await service.InviteUserAsync(party.Id, leader.Id, member.Id);
        await service.RespondToUserInviteAsync(invite.Id, member.Id, true);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.InviteUserAsync(party.Id, leader.Id, member.Id));

        Assert.AreEqual("User is already in party.", ex.Message);
    }

    [TestMethod]
    public async Task InviteUser_BlockedUserCannotBeInvited_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);
        await service.BlockUserAsync(leader.Id, invited.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.InviteUserAsync(party.Id, leader.Id, invited.Id));

        Assert.AreEqual("You have blocked this user.", ex.Message);
    }

    [TestMethod]
    public async Task InviteUser_PartyFullAtFiveMembers_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var party = await service.CreatePartyAsync(leader.Id);

        for (int i = 0; i < 4; i++)
        {
            var m = await SeedUserAsync(context, $"member{i}");
            var inv = await service.InviteUserAsync(party.Id, leader.Id, m.Id);
            await service.RespondToUserInviteAsync(inv.Id, m.Id, true);
        }

        var extra = await SeedUserAsync(context, "extra");
        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.InviteUserAsync(party.Id, leader.Id, extra.Id));

        Assert.AreEqual("Party is full. (Max 5 Members)", ex.Message);
    }

    [TestMethod]
    public async Task InviteUser_PendingInviteAlreadyExists_ThrowsPartyValidationException()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");
        var invited = await SeedUserAsync(context, "invited");
        var party = await service.CreatePartyAsync(leader.Id);

        await service.InviteUserAsync(party.Id, leader.Id, invited.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            service.InviteUserAsync(party.Id, leader.Id, invited.Id));

        Assert.AreEqual("An invite is already pending for this user.", ex.Message);
    }
}
