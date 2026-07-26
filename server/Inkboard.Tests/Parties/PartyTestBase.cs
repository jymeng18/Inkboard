using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Moq;

namespace Inkboard.Tests.Parties;

public abstract class PartyTestBase : TestBase
{
    protected AppDbContext Context { get; private set; } = null!;
    protected PartyService Service { get; private set; } = null!;

    [TestInitialize]
    public void BaseInitialize()
    {
        Context = CreateDbContext();
        Service = CreatePartyService(Context);
    }

    [TestCleanup]
    public void BaseCleanup()
    {
        Context.Dispose();
    }

    protected static PartyService CreatePartyService(AppDbContext context)
    {
        var notifierMock = new Mock<IPartyNotifier>(MockBehavior.Loose);
        var canvasRepository = new CanvasRepository(context);
        var canvasService = new CanvasService(canvasRepository, new PartyRepository(context));
        return new PartyService(
            new PartyRepository(context),
            new PartyInviteRepository(context),
            new BlockListRepository(context),
            notifierMock.Object,
            canvasService,
            canvasRepository
        );
    }

    protected static PartyService CreatePartyServiceWithNotifier(
        AppDbContext context,
        out Mock<IPartyNotifier> notifierMock
    )
    {
        notifierMock = new Mock<IPartyNotifier>(MockBehavior.Loose);
        var canvasRepository = new CanvasRepository(context);
        var canvasService = new CanvasService(canvasRepository, new PartyRepository(context));
        return new PartyService(
            new PartyRepository(context),
            new PartyInviteRepository(context),
            new BlockListRepository(context),
            notifierMock.Object,
            canvasService,
            canvasRepository
        );
    }

    protected static async Task<User> SeedUserAsync(AppDbContext context, string userName)
    {
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}@test.com",
            PasswordHash = "hash",
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    protected static async Task<Canvas> SeedCanvasAsync(
        AppDbContext context,
        Guid ownerId,
        string name = "Test Canvas"
    )
    {
        var canvas = new Canvas
        {
            OwnerId = ownerId,
            Name = name,
            LastModifiedAt = DateTime.UtcNow,
        };
        context.Canvas.Add(canvas);
        await context.SaveChangesAsync();
        return canvas;
    }

    protected static async Task<(Party Party, Canvas Canvas)> SeedPartyAsync(
        AppDbContext context,
        Guid leaderId,
        string canvasName = "Test Canvas"
    )
    {
        var canvas = await SeedCanvasAsync(context, leaderId, canvasName);
        var partyRepo = new PartyRepository(context);
        var party = new Party
        {
            LeaderId = leaderId,
            CanvasId = canvas.Id,
            CreatedAt = DateTime.UtcNow,
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
