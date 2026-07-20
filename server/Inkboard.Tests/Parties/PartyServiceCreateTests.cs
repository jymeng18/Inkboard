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

        var result = await Service.CreatePartyAsync(leader.Id);
        Assert.IsTrue(result.IsSuccess);
        var party = result.Data!;

        Assert.AreEqual(leader.Id, party.LeaderId);
        Assert.AreNotEqual(Guid.Empty, party.Id);

        var member = await Context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == leader.Id);
        Assert.IsNotNull(member);
        Assert.AreEqual(UserRole.Leader, member.Role);
    }
}
