using Inkboard.Application.Common;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Canvases;

/// <summary>
/// The persistence and access rules for canvas operations. Two rules matter most:
/// an operation payload is opaque but bounded (1 byte to 256KB), and only the
/// canvas owner or a party member whose party is currently pointed at that canvas
/// may read or write its op-log.
/// </summary>
[TestClass]
public sealed class CanvasOperationTests : CanvasTestBase
{
    private const int MaxOperationDataLength = 256 * 1024;

    // ─── SaveOperation: payload bounds ──────────────────────────

    [TestMethod]
    public async Task SaveOperation_OwnerSoloNoParty_PersistsOperation()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, owner.Id, (int)ActionType.Draw, "{\"op\":1}");

        Assert.IsTrue(result.IsSuccess);
        var saved = await Context.CanvasOperations.SingleAsync(o => o.CanvasId == canvas.Id);
        Assert.AreEqual("{\"op\":1}", saved.OperationData);
        Assert.AreEqual(owner.Id, saved.UserId);
    }

    [TestMethod]
    public async Task SaveOperation_EmptyPayload_ReturnsValidation()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, owner.Id, 0, "");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.IsFalse(await Context.CanvasOperations.AnyAsync(o => o.CanvasId == canvas.Id));
    }

    [TestMethod]
    public async Task SaveOperation_PayloadOneOverMax_ReturnsValidation()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();
        var tooBig = new string('x', MaxOperationDataLength + 1);

        var result = await service.SaveOperationAsync(canvas.Id, owner.Id, 0, tooBig);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.IsFalse(await Context.CanvasOperations.AnyAsync(o => o.CanvasId == canvas.Id));
    }

    [TestMethod]
    public async Task SaveOperation_PayloadAtMax_Persists()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();
        var atLimit = new string('x', MaxOperationDataLength);

        var result = await service.SaveOperationAsync(canvas.Id, owner.Id, 0, atLimit);

        Assert.IsTrue(result.IsSuccess);
        var saved = await Context.CanvasOperations.SingleAsync(o => o.CanvasId == canvas.Id);
        Assert.AreEqual(MaxOperationDataLength, saved.OperationData.Length);
    }

    [TestMethod]
    public async Task SaveOperation_CanvasNotFound_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(Guid.NewGuid(), owner.Id, 0, "{}");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    // ─── SaveOperation: access control ──────────────────────────

    [TestMethod]
    public async Task SaveOperation_NonOwnerWithNoParty_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var stranger = await SeedUserAsync("stranger");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, stranger.Id, 0, "{}");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.IsFalse(await Context.CanvasOperations.AnyAsync(o => o.CanvasId == canvas.Id));
    }

    [TestMethod]
    public async Task SaveOperation_PartyMemberOnMatchingCanvas_Persists()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id);
        await SeedPartyWithMemberAsync(owner.Id, member.Id, canvas.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, member.Id, 0, "{\"stroke\":true}");

        Assert.IsTrue(result.IsSuccess);
        var saved = await Context.CanvasOperations.SingleAsync(o => o.CanvasId == canvas.Id);
        Assert.AreEqual(member.Id, saved.UserId);
    }

    [TestMethod]
    public async Task SaveOperation_PartyMemberOnDifferentCanvas_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id);
        var otherCanvas = await SeedCanvasAsync(owner.Id, "Other");
        // Member's party is pointed at a different canvas than the one written to.
        await SeedPartyWithMemberAsync(owner.Id, member.Id, otherCanvas.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, member.Id, 0, "{}");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task SaveOperation_MemberWhoseSessionWasForceEnded_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id);
        // Session ended: the party still exists but its canvas link was severed (null).
        await SeedPartyWithMemberAsync(owner.Id, member.Id, canvasId: null);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, member.Id, 0, "{}");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    // ─── SaveOperation: ActionType clamping ─────────────────────

    [TestMethod]
    public async Task SaveOperation_KnownEraseType_PersistsAsErase()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, owner.Id, (int)ActionType.Erase, "{}");

        Assert.IsTrue(result.IsSuccess);
        var saved = await Context.CanvasOperations.SingleAsync(o => o.CanvasId == canvas.Id);
        Assert.AreEqual(ActionType.Erase, saved.Type);
    }

    [TestMethod]
    public async Task SaveOperation_UndefinedTypeInt_DefaultsToDraw()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, owner.Id, 99, "{}");

        Assert.IsTrue(result.IsSuccess);
        var saved = await Context.CanvasOperations.SingleAsync(o => o.CanvasId == canvas.Id);
        Assert.AreEqual(ActionType.Draw, saved.Type);
    }

    [TestMethod]
    public async Task SaveOperation_NegativeType_DefaultsToDraw()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.SaveOperationAsync(canvas.Id, owner.Id, -5, "{}");

        Assert.IsTrue(result.IsSuccess);
        var saved = await Context.CanvasOperations.SingleAsync(o => o.CanvasId == canvas.Id);
        Assert.AreEqual(ActionType.Draw, saved.Type);
    }

    // ─── GetOperations: access + projection + ordering ──────────

    [TestMethod]
    public async Task GetOperations_CanvasNotFound_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(Guid.NewGuid(), owner.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task GetOperations_OwnerEmptyLog_ReturnsEmptyList()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Data!);
    }

    [TestMethod]
    public async Task GetOperations_OwnerRetrieves_ReturnsOnlyPayloadStrings()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        await SeedOperationAsync(canvas.Id, owner.Id, "{\"a\":1}");
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(new[] { "{\"a\":1}" }, result.Data!);
    }

    [TestMethod]
    public async Task GetOperations_ReturnsPayloadsInTimestampOrder()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var now = DateTimeOffset.UtcNow;
        // Seed out of chronological order to prove the read sorts by timestamp.
        await SeedOperationAsync(canvas.Id, owner.Id, "third", timestamp: now.AddSeconds(3));
        await SeedOperationAsync(canvas.Id, owner.Id, "first", timestamp: now.AddSeconds(1));
        await SeedOperationAsync(canvas.Id, owner.Id, "second", timestamp: now.AddSeconds(2));
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, result.Data!);
    }

    [TestMethod]
    public async Task GetOperations_ScopedToSingleCanvas_ExcludesOtherCanvasOps()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var otherCanvas = await SeedCanvasAsync(owner.Id, "Other");
        await SeedOperationAsync(canvas.Id, owner.Id, "mine");
        await SeedOperationAsync(otherCanvas.Id, owner.Id, "theirs");
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(new[] { "mine" }, result.Data!);
    }

    [TestMethod]
    public async Task GetOperations_NonOwnerWithNoParty_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var stranger = await SeedUserAsync("stranger");
        var canvas = await SeedCanvasAsync(owner.Id);
        await SeedOperationAsync(canvas.Id, owner.Id, "secret");
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, stranger.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task GetOperations_PartyMemberOnMatchingCanvas_ReturnsPayloads()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id);
        await SeedPartyWithMemberAsync(owner.Id, member.Id, canvas.Id);
        await SeedOperationAsync(canvas.Id, owner.Id, "shared");
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, member.Id);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(new[] { "shared" }, result.Data!);
    }

    [TestMethod]
    public async Task GetOperations_PartyMemberOnDifferentCanvas_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id);
        var otherCanvas = await SeedCanvasAsync(owner.Id, "Other");
        await SeedPartyWithMemberAsync(owner.Id, member.Id, otherCanvas.Id);
        await SeedOperationAsync(canvas.Id, owner.Id, "secret");
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, member.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task GetOperations_OwnerInPartyElsewhere_StillReadsOwnCanvas()
    {
        var owner = await SeedUserAsync("owner");
        var friendLeader = await SeedUserAsync("friendLeader");
        var canvas = await SeedCanvasAsync(owner.Id);
        var otherCanvas = await SeedCanvasAsync(friendLeader.Id, "Friends");
        // The owner is off in someone else's party on another canvas, but ownership
        // of their own canvas is absolute and independent of any party link.
        await SeedPartyWithMemberAsync(friendLeader.Id, owner.Id, otherCanvas.Id);
        await SeedOperationAsync(canvas.Id, owner.Id, "own-op");
        var service = CreateCanvasService();

        var result = await service.GetOperationsAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(new[] { "own-op" }, result.Data!);
    }
}
