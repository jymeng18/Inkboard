using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Workflows;

// Persistence integration checks for the account-deletion retention policy.
[TestClass]
public sealed class AccountDeletionPersistenceTests : TestBase
{
    [TestMethod]
    public async Task DeleteUserAsync_WhenUserContributedToAnotherCanvas_PreservesDrawingWithoutAttribution()
    {
        var owner = await SeedUserAsync("owner");
        var contributor = await SeedUserAsync("contributor");
        var canvas = await SeedCanvasAsync(owner.Id);
        var operation = new CanvasOperation { CanvasId = canvas.Id, UserId = contributor.Id, OperationData = "shared drawing" };
        Context.CanvasOperations.Add(operation);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var repository = new UserRepository(Context);
        var account = await repository.GetByIdAsync(contributor.Id);

        await repository.DeleteUserAsync(account!);

        var retained = await Context.CanvasOperations.SingleAsync();
        Assert.AreEqual("shared drawing", retained.OperationData);
        Assert.IsNull(retained.UserId);
    }

    [TestMethod]
    public async Task AddRangeAsync_WhenContributorDeletedBeforeQueueFlush_PreservesQueuedDrawingAnonymously()
    {
        var owner = await SeedUserAsync("owner");
        var contributor = await SeedUserAsync("contributor");
        var canvas = await SeedCanvasAsync(owner.Id);
        var operation = new CanvasOperation { CanvasId = canvas.Id, UserId = contributor.Id, OperationData = "queued drawing" };
        await new UserRepository(Context).DeleteUserAsync(contributor);

        await new OperationRepository(Context).AddRangeAsync(new[] { operation });

        var retained = await Context.CanvasOperations.SingleAsync();
        Assert.AreEqual("queued drawing", retained.OperationData);
        Assert.IsNull(retained.UserId);
    }

    [TestMethod]
    public async Task AddRangeAsync_WhenOneCanvasDeleted_KeepsQueuedDrawingForSurvivingCanvas()
    {
        var deletedOwner = await SeedUserAsync("deletedOwner");
        var survivingOwner = await SeedUserAsync("survivingOwner");
        var deletedCanvas = await SeedCanvasAsync(deletedOwner.Id);
        var survivingCanvas = await SeedCanvasAsync(survivingOwner.Id);
        var operations = new[]
        {
            new CanvasOperation { CanvasId = deletedCanvas.Id, UserId = deletedOwner.Id, OperationData = "removed canvas" },
            new CanvasOperation { CanvasId = survivingCanvas.Id, UserId = survivingOwner.Id, OperationData = "retained drawing" },
        };
        await new UserRepository(Context).DeleteUserAsync(deletedOwner);

        await new OperationRepository(Context).AddRangeAsync(operations);

        var retained = await Context.CanvasOperations.SingleAsync();
        Assert.AreEqual(survivingCanvas.Id, retained.CanvasId);
        Assert.AreEqual("retained drawing", retained.OperationData);
    }
}
