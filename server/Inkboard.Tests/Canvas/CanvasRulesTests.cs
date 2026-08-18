using Inkboard.Application.Common;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Canvases;

/// <summary>
/// Canvas naming limits, the force-end session guards, and the standalone PNG
/// header validator. The name cap (50 chars) and the image bounds (1KB–10MB,
/// 50–8192px, real PNG signature) are the input rules that protect storage.
/// </summary>
[TestClass]
public sealed class CanvasRulesTests : CanvasTestBase
{
    // ─── CreateCanvas / Rename name length ──────────────────────

    [TestMethod]
    public async Task CreateCanvas_NameOverFifty_ReturnsValidation()
    {
        var owner = await SeedUserAsync("owner");
        var service = CreateCanvasService();

        var result = await service.CreateCanvasAsync(owner.Id, new string('n', 51));

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task CreateCanvas_NameAtFifty_Succeeds()
    {
        var owner = await SeedUserAsync("owner");
        var service = CreateCanvasService();

        var result = await service.CreateCanvasAsync(owner.Id, new string('n', 50));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(50, result.Data!.Name!.Length);
    }

    [TestMethod]
    public async Task Rename_NameOverFifty_ReturnsValidation()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, "Original");
        var service = CreateCanvasService();

        var result = await service.RenameCanvas(new string('n', 51), canvas.Id, owner.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        var reloaded = await Context.Canvas.AsNoTracking().SingleAsync(c => c.Id == canvas.Id);
        Assert.AreEqual("Original", reloaded.Name);
    }

    [TestMethod]
    public async Task Rename_OwnerWithinLimit_UpdatesNameAndLastModified()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, "Original");
        var before = DateTimeOffset.UtcNow;
        var service = CreateCanvasService();

        var result = await service.RenameCanvas("Renamed", canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        var reloaded = await Context.Canvas.AsNoTracking().SingleAsync(c => c.Id == canvas.Id);
        Assert.AreEqual("Renamed", reloaded.Name);
        Assert.IsTrue(reloaded.LastModifiedAt >= before);
    }

    // ─── ForceEndSession guards ─────────────────────────────────

    [TestMethod]
    public async Task ForceEndSession_CanvasNotFound_ReturnsNotFound()
    {
        var user = await SeedUserAsync("user");
        var service = CreateCanvasService();

        var result = await service.ForceEndSessionAsync(Guid.NewGuid(), user.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task ForceEndSession_UserHasNoActiveParty_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService();

        var result = await service.ForceEndSessionAsync(canvas.Id, owner.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Party not found.", result.Error);
    }

    [TestMethod]
    public async Task ForceEndSession_LeaderEndsOwnSession_SeversLinkKeepsCanvas()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var party = await SeedPartyWithMemberAsync(owner.Id, (await SeedUserAsync("m")).Id, canvas.Id);
        var service = CreateCanvasService();

        var result = await service.ForceEndSessionAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        var reloadedParty = await Context.Parties.AsNoTracking().SingleAsync(p => p.Id == party.Id);
        Assert.IsNull(reloadedParty.CanvasId);
        Assert.IsTrue(await Context.Canvas.AnyAsync(c => c.Id == canvas.Id));
    }

    // ─── ImageDataValidator (pure header/size checks) ───────────

    [TestMethod]
    public async Task ImageValidator_WellFormedPng_Succeeds()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(100, 100));
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ImageValidator_NullStream_ReturnsValidation()
    {
        var result = await CanvasService.ImageDataValidator(null!);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_BelowOneKilobyte_ReturnsValidation()
    {
        var result = await CanvasService.ImageDataValidator(new MemoryStream(new byte[512]));
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_AboveTenMegabytes_ReturnsValidation()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(100, 100, 10 * 1024 * 1024 + 1));
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_WrongSignature_ReturnsValidation()
    {
        // Right size, but the leading 8 bytes are not the PNG magic number.
        var result = await CanvasService.ImageDataValidator(new MemoryStream(new byte[2048]));
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_WidthBelowFifty_ReturnsValidation()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(49, 100));
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_HeightBelowFifty_ReturnsValidation()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(100, 49));
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_WidthAboveMax_ReturnsValidation()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(8193, 100));
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_HeightAboveMax_ReturnsValidation()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(100, 8193));
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task ImageValidator_DimensionsAtLowerBound_Succeeds()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(50, 50));
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ImageValidator_DimensionsAtUpperBound_Succeeds()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(8192, 8192));
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ImageValidator_SizeAtLowerBound_Succeeds()
    {
        var result = await CanvasService.ImageDataValidator(BuildPng(100, 100, 1024));
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ImageValidator_LeavesStreamPositionAtStart()
    {
        // The service reads the payload again after validating, so the validator
        // must rewind whatever it consumed for the header peek.
        var png = BuildPng(100, 100);
        await CanvasService.ImageDataValidator(png);
        Assert.AreEqual(0, png.Position);
    }
}
