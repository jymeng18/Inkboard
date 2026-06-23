using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

public abstract class PartyTestBase : TestBase
{
    protected static PartyService CreatePartyService(AppDbContext context)
    {
        return new PartyService(
            new PartyRepository(context),
            new PartyInviteRepository(context),
            new BlockListRepository(context)
        );
    }

    protected static async Task<T> AssertThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T ex)
        {
            return ex;
        }
        throw new AssertFailedException($"Expected {typeof(T).Name} but no exception was thrown.");
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
