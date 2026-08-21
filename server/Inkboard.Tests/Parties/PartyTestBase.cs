using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Moq;

namespace Inkboard.Tests.Parties;

public abstract class PartyTestBase : TestBase
{
    protected PartyService Service { get; private set; } = null!;

    /// <summary>The notifier <see cref="Service"/> is wired with; loose, so tests
    /// that don't care about notifications ignore it and the rest verify against it.</summary>
    protected Mock<IPartyNotifier> Notifier { get; private set; } = null!;

    [TestInitialize]
    public void InitPartyService()
    {
        Notifier = new Mock<IPartyNotifier>(MockBehavior.Loose);
        var canvasRepository = new CanvasRepository(Context);
        var canvasService = new CanvasService(canvasRepository, new PartyRepository(Context), null, null, null);
        Service = new PartyService(
            new PartyRepository(Context),
            new PartyInviteRepository(Context),
            new BlockListRepository(Context),
            Notifier.Object,
            canvasService,
            canvasRepository
        );
    }

    /// <summary>A CanvasService wired with real repos and no blob/image/operation
    /// collaborators, enough for the create/rename/delete/link rules these tests exercise.</summary>
    protected CanvasService CreateCanvasService()
    {
        return new CanvasService(
            new CanvasRepository(Context),
            new PartyRepository(Context),
            null,
            null,
            null
        );
    }

    protected async Task<(Party Party, Canvas Canvas)> SeedPartyAsync(
        Guid leaderId,
        string canvasName = "Test Canvas"
    )
    {
        var canvas = await SeedCanvasAsync(leaderId, canvasName);
        var partyRepo = new PartyRepository(Context);
        var party = new Party
        {
            LeaderId = leaderId,
            CanvasId = canvas.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await partyRepo.CreatePartyAsync(party);
        var member = new PartyMember
        {
            PartyId = party.Id,
            UserId = leaderId,
            Role = UserRole.Leader,
        };
        await partyRepo.AddMemberAsync(member);
        return (party, canvas);
    }
}
