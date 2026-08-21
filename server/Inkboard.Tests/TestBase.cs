using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests;

/// <summary>
/// Base for every test that talks to the database. Owns a fresh in-memory
/// <see cref="AppDbContext"/> per test and the seeding helpers shared across
/// domains. Domain-specific bases add their own service wiring on top.
/// </summary>
public abstract class TestBase
{
    protected AppDbContext Context { get; private set; } = null!;

    [TestInitialize]
    public void InitializeContext()
    {
        Context = CreateDbContext();
    }

    [TestCleanup]
    public void DisposeContext()
    {
        Context?.Dispose();
    }

    protected static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    protected async Task<User> SeedUserAsync(string userName)
    {
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}-{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
        };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    protected async Task<Canvas> SeedCanvasAsync(
        Guid ownerId,
        string name = "Test Canvas",
        string? snapshotUrl = null
    )
    {
        var canvas = new Canvas
        {
            OwnerId = ownerId,
            Name = name,
            SnapshotURL = snapshotUrl,
            LastModifiedAt = DateTimeOffset.UtcNow,
        };
        Context.Canvas.Add(canvas);
        await Context.SaveChangesAsync();
        return canvas;
    }
}
