using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceCreateTests : PartyTestBase
{
    [TestMethod]
    public async Task CreateParty_CreatesParty_LeaderIsMemberWithLeaderRole()
    {
        var context = CreateDbContext();
        var service = CreatePartyService(context);
        var leader = await SeedUserAsync(context, "leader");

        var party = await service.CreatePartyAsync(leader.Id);

        Assert.IsNotNull(party);
        Assert.AreEqual(leader.Id, party.LeaderId);
        Assert.AreNotEqual(Guid.Empty, party.Id);

        var member = await context.PartyMembers
            .FirstOrDefaultAsync(pm => pm.PartyId == party.Id && pm.UserId == leader.Id);
        Assert.IsNotNull(member);
        Assert.AreEqual(UserRole.Leader, member.Role);
    }
}
