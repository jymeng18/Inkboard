using Inkboard.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceBlockTests : PartyTestBase
{
    [TestMethod]
    public async Task BlockUser_BlocksAnotherUser_AddsToBlockList()
    {
        var user = await SeedUserAsync(Context, "user");
        var target = await SeedUserAsync(Context, "target");

        var result = await Service.BlockUserAsync(user.Id, target.Id);
        Assert.IsTrue(result.IsSuccess);

        var isBlocked = await Context.BlockLists
            .AnyAsync(bl => bl.UserId == user.Id && bl.BlockedUserId == target.Id);
        Assert.IsTrue(isBlocked);
    }

    [TestMethod]
    public async Task BlockUser_CannotBlockSelf_ReturnsValidationError()
    {
        var user = await SeedUserAsync(Context, "user");

        var result = await Service.BlockUserAsync(user.Id, user.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You cannot block yourself.", result.Error);
    }

    [TestMethod]
    public async Task BlockUser_CannotBlockAlreadyBlockedUser_ReturnsConflict()
    {
        var user = await SeedUserAsync(Context, "user");
        var target = await SeedUserAsync(Context, "target");

        var firstResult = await Service.BlockUserAsync(user.Id, target.Id);
        Assert.IsTrue(firstResult.IsSuccess);

        var result = await Service.BlockUserAsync(user.Id, target.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("This user is already blocked.", result.Error);
    }
}
