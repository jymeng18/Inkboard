using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceCreateTests : PartyTestBase
{
    [TestMethod]
    public async Task CreateParty_CreatesParty_LeaderIsMemberWithLeaderRole()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);

        var result = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(result.IsSuccess);
        var party = result.Data!;

        Assert.AreEqual(leader.Id, party.LeaderId);
        Assert.AreEqual(canvas.Id, party.CanvasId);
        Assert.AreNotEqual(Guid.Empty, party.Id);

        var member = await Context.PartyMembers.FirstOrDefaultAsync(pm =>
            pm.PartyId == party.Id && pm.UserId == leader.Id
        );
        Assert.IsNotNull(member);
        Assert.AreEqual(UserRole.Leader, member.Role);
    }

    [TestMethod]
    public async Task CreateParty_UserAlreadyInActiveParty_ReturnsConflict()
    {
        var leader = await SeedUserAsync(Context, "leader");
        var canvas = await SeedCanvasAsync(Context, leader.Id);
        var first = await Service.CreatePartyAsync(leader.Id, canvas.Id);
        Assert.IsTrue(first.IsSuccess);

        var canvas2 = await SeedCanvasAsync(Context, leader.Id, "Another Canvas");
        var result = await Service.CreatePartyAsync(leader.Id, canvas2.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("An active party already exists.", result.Error);
    }
}
