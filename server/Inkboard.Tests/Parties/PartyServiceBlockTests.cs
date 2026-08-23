using Inkboard.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceBlockTests : PartyTestBase
{
    [TestMethod]
    public async Task BlockUser_BlocksAnotherUser_AddsToBlockList()
    {
        var user = await SeedUserAsync("user");
        var target = await SeedUserAsync("target");

        var result = await Service.BlockUserAsync(user.Id, target.Id);
        Assert.IsTrue(result.IsSuccess);

        var isBlocked = await Context.BlockLists.AnyAsync(bl =>
            bl.UserId == user.Id && bl.BlockedUserId == target.Id
        );
        Assert.IsTrue(isBlocked);
    }

    [TestMethod]
    public async Task BlockUser_CannotBlockSelf_ReturnsValidationError()
    {
        var user = await SeedUserAsync("user");

        var result = await Service.BlockUserAsync(user.Id, user.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You cannot block yourself.", result.Error);
    }

    [TestMethod]
    public async Task BlockUser_CannotBlockAlreadyBlockedUser_ReturnsConflict()
    {
        var user = await SeedUserAsync("user");
        var target = await SeedUserAsync("target");

        var firstResult = await Service.BlockUserAsync(user.Id, target.Id);
        Assert.IsTrue(firstResult.IsSuccess);

        var result = await Service.BlockUserAsync(user.Id, target.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("This user is already blocked.", result.Error);
    }

    [TestMethod]
    public async Task BlockUser_BlockedUserCanUnblockThenBeInvited()
    {
        var leader = await SeedUserAsync("leader");
        var invited = await SeedUserAsync("invited");
        var canvas = await SeedCanvasAsync(leader.Id);
        var partyResult = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(partyResult.IsSuccess);
        var party = partyResult.Data!;

        var blockResult = await Service.BlockUserAsync(leader.Id, invited.Id);
        Assert.IsTrue(blockResult.IsSuccess);

        var inviteResult = await Service.InviteUserAsync(party.Id, leader.Id, invited.Id);
        Assert.IsFalse(inviteResult.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, inviteResult.ErrorType);
        Assert.AreEqual("You have blocked this user.", inviteResult.Error);
    }
}
