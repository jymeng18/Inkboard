using Inkboard.API.Hubs;
using Inkboard.API.Realtime;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
        return new PartyService(
            new PartyRepository(context),
            new PartyInviteRepository(context),
            new BlockListRepository(context),
            notifierMock.Object
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
}
