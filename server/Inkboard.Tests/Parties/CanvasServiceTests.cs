using Inkboard.Application.Common;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class CanvasServiceTests : PartyTestBase
{
    private CanvasService CreateCanvasService()
    {
        return new CanvasService(new CanvasRepository(Context), new PartyRepository(Context));
    }

    [TestMethod]
    public async Task CreateCanvasAsync_CreatesCanvas_WithCorrectOwnerAndName()
    {
        var user = await SeedUserAsync(Context, "user");
        var canvasService = CreateCanvasService();

        var result = await canvasService.CreateCanvasAsync(user.Id, "My Canvas");
        Assert.IsTrue(result.IsSuccess);
        var canvas = result.Data!;

        Assert.AreEqual(user.Id, canvas.OwnerId);
        Assert.AreEqual("My Canvas", canvas.Name);
        Assert.IsNull(canvas.SnapshotURL);
        Assert.AreNotEqual(Guid.Empty, canvas.Id);

        var saved = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(saved);
        Assert.AreEqual(user.Id, saved.OwnerId);
    }

    [TestMethod]
    public async Task DeleteCanvasAsync_OwnerDeletes_CanvasRemoved()
    {
        var user = await SeedUserAsync(Context, "user");
        var canvas = await SeedCanvasAsync(Context, user.Id, "Delete Me");
        var canvasService = CreateCanvasService();

        var result = await canvasService.DeleteCanvasAsync(canvas.Id, user.Id);
        Assert.IsTrue(result.IsSuccess);

        var deleted = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task DeleteCanvasAsync_NonOwnerCannotDelete_ReturnsForbidden()
    {
        var owner = await SeedUserAsync(Context, "owner");
        var other = await SeedUserAsync(Context, "other");
        var canvas = await SeedCanvasAsync(Context, owner.Id, "Owner Canvas");
        var canvasService = CreateCanvasService();

        var result = await canvasService.DeleteCanvasAsync(canvas.Id, other.Id);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Unauthorized deletion on this canvas.", result.Error);

        var stillExists = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(stillExists);
    }

    [TestMethod]
    public async Task DeleteCanvasAsync_CanvasNotFound_ReturnsNotFound()
    {
        var user = await SeedUserAsync(Context, "user");
        var canvasService = CreateCanvasService();

        var result = await canvasService.DeleteCanvasAsync(Guid.NewGuid(), user.Id);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Canvas not found.", result.Error);
    }

    [TestMethod]
    public async Task RenameCanvas_OwnerRenames_NameChanged()
    {
        var user = await SeedUserAsync(Context, "user");
        var canvas = await SeedCanvasAsync(Context, user.Id, "Old Name");
        var canvasService = CreateCanvasService();

        var result = await canvasService.RenameCanvas("New Name", canvas.Id, user.Id);
        Assert.IsTrue(result.IsSuccess);

        var updated = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("New Name", updated.Name);
    }

    [TestMethod]
    public async Task RenameCanvas_NonOwnerCannotRename_ReturnsForbidden()
    {
        var owner = await SeedUserAsync(Context, "owner");
        var other = await SeedUserAsync(Context, "other");
        var canvas = await SeedCanvasAsync(Context, owner.Id, "Owner Canvas");
        var canvasService = CreateCanvasService();

        var result = await canvasService.RenameCanvas("Hacked Name", canvas.Id, other.Id);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Canvas does not belong to you.", result.Error);

        var unchanged = await Context.Canvas.FindAsync(canvas.Id);
        Assert.IsNotNull(unchanged);
        Assert.AreEqual("Owner Canvas", unchanged.Name);
    }

    [TestMethod]
    public async Task RenameCanvas_CanvasNotFound_ReturnsNotFound()
    {
        var user = await SeedUserAsync(Context, "user");
        var canvasService = CreateCanvasService();

        var result = await canvasService.RenameCanvas("Any", Guid.NewGuid(), user.Id);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Canvas not found.", result.Error);
    }

    [TestMethod]
    public async Task GetAllCanvasesAsync_ReturnsOnlyUserCanvases()
    {
        var userA = await SeedUserAsync(Context, "userA");
        var userB = await SeedUserAsync(Context, "userB");
        await SeedCanvasAsync(Context, userA.Id, "A1");
        await SeedCanvasAsync(Context, userA.Id, "A2");
        await SeedCanvasAsync(Context, userB.Id, "B1");
        var canvasService = CreateCanvasService();

        var result = await canvasService.GetAllCanvasesAsync(userA.Id);
        Assert.IsTrue(result.IsSuccess);
        var canvases = result.Data!;
        Assert.AreEqual(2, canvases.Count);
        Assert.IsTrue(canvases.All(c => c.OwnerId == userA.Id));
    }

    [TestMethod]
    public async Task GetAllCanvasesAsync_ReturnsEmptyForUserWithNoCanvases()
    {
        var user = await SeedUserAsync(Context, "user");
        var canvasService = CreateCanvasService();

        var result = await canvasService.GetAllCanvasesAsync(user.Id);
        Assert.IsTrue(result.IsSuccess);
        var canvases = result.Data!;
        Assert.AreEqual(0, canvases.Count);
    }

    [TestMethod]
    public async Task CreateCanvas_CreatesMultipleCanvases_SameOwner()
    {
        var user = await SeedUserAsync(Context, "user");
        var canvasService = CreateCanvasService();

        var r1 = await canvasService.CreateCanvasAsync(user.Id, "Canvas 1");
        var r2 = await canvasService.CreateCanvasAsync(user.Id, "Canvas 2");
        Assert.IsTrue(r1.IsSuccess);
        Assert.IsTrue(r2.IsSuccess);

        var allCanvases = await canvasService.GetAllCanvasesAsync(user.Id);
        Assert.AreEqual(2, allCanvases.Data!.Count);
    }
}
