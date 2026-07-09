using Inkboard.Application.Interfaces;
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

        await Service.BlockUserAsync(user.Id, target.Id);

        var isBlocked = await Context.BlockLists
            .AnyAsync(bl => bl.UserId == user.Id && bl.BlockedUserId == target.Id);
        Assert.IsTrue(isBlocked);
    }

    [TestMethod]
    public async Task BlockUser_CannotBlockSelf_ThrowsPartyValidationException()
    {
        var user = await SeedUserAsync(Context, "user");

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.BlockUserAsync(user.Id, user.Id));

        Assert.AreEqual("You cannot block yourself.", ex.Message);
    }

    [TestMethod]
    public async Task BlockUser_CannotBlockAlreadyBlockedUser_ThrowsPartyValidationException()
    {
        var user = await SeedUserAsync(Context, "user");
        var target = await SeedUserAsync(Context, "target");

        await Service.BlockUserAsync(user.Id, target.Id);

        var ex = await AssertThrowsAsync<PartyValidationException>(() =>
            Service.BlockUserAsync(user.Id, target.Id));

        Assert.AreEqual("This user is already blocked.", ex.Message);
    }
}
