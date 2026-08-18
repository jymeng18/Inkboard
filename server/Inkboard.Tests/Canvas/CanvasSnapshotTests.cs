using Inkboard.Application.Common;
using Inkboard.Application.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Inkboard.Tests.Canvases;

/// <summary>
/// Snapshot read and write rules. The authoritative full snapshot is readable by
/// the owner or a party member on the matching canvas; the downscaled preview is
/// owner-only. Uploads are owner-only, must be PNG, and only mutate the canvas
/// row once the payload has both passed the header check and decoded.
/// </summary>
[TestClass]
public sealed class CanvasSnapshotTests : CanvasTestBase
{
    private const string SnapshotUrl = "https://blob.test/snap.png";

    // ─── GetSnapshotAsync ───────────────────────────────────────

    [TestMethod]
    public async Task GetSnapshot_CanvasNotFound_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotAsync(Guid.NewGuid(), owner.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task GetSnapshot_NoSnapshotTaken_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: null);
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotAsync(canvas.Id, owner.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Snapshot not found.", result.Error);
    }

    [TestMethod]
    public async Task GetSnapshot_OwnerWithSnapshot_ReturnsStream()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        var blob = CreateBlobMock();
        blob.Setup(b => b.DownloadSnapshotAsync(canvas.Id))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));
        var service = CreateCanvasService(blob.Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
    }

    [TestMethod]
    public async Task GetSnapshot_NonOwnerNotInParty_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var stranger = await SeedUserAsync("stranger");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotAsync(canvas.Id, stranger.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task GetSnapshot_PartyMemberOnMatchingCanvas_ReturnsStream()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        await SeedPartyWithMemberAsync(owner.Id, member.Id, canvas.Id);
        var blob = CreateBlobMock();
        blob.Setup(b => b.DownloadSnapshotAsync(canvas.Id))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));
        var service = CreateCanvasService(blob.Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotAsync(canvas.Id, member.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
    }

    [TestMethod]
    public async Task GetSnapshot_PartyMemberOnDifferentCanvas_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        var otherCanvas = await SeedCanvasAsync(owner.Id, "Other");
        await SeedPartyWithMemberAsync(owner.Id, member.Id, otherCanvas.Id);
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotAsync(canvas.Id, member.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    // ─── GetSnapshotPreviewAsync ────────────────────────────────

    [TestMethod]
    public async Task GetSnapshotPreview_NonOwner_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        // Even a valid party member on this exact canvas cannot read the preview:
        // the preview is strictly owner-only, unlike the full snapshot.
        await SeedPartyWithMemberAsync(owner.Id, member.Id, canvas.Id);
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotPreviewAsync(canvas.Id, member.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task GetSnapshotPreview_NoSnapshotTaken_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: null);
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotPreviewAsync(canvas.Id, owner.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task GetSnapshotPreview_PrebuiltPreviewExists_ReturnsItWithoutFallback()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        var blob = CreateBlobMock();
        blob.Setup(b => b.DownloadPreviewAsync(canvas.Id))
            .ReturnsAsync(new MemoryStream([9, 9]));
        // Strict blob mock has no snapshot-download setup, so the fast path must not
        // touch the full image.
        var service = CreateCanvasService(blob.Object, CreateImageValidatorMock().Object);

        var result = await service.GetSnapshotPreviewAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        blob.Verify(b => b.DownloadSnapshotAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task GetSnapshotPreview_NoPreview_FallsBackToDownscalingSnapshot()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        var blob = CreateBlobMock();
        blob.Setup(b => b.DownloadPreviewAsync(canvas.Id)).ReturnsAsync((Stream?)null);
        blob.Setup(b => b.DownloadSnapshotAsync(canvas.Id)).ReturnsAsync(new MemoryStream([1, 2, 3, 4]));
        var validator = CreateImageValidatorMock();
        validator.Setup(v => v.CreatePngThumbnailAsync(It.IsAny<Stream>(), It.IsAny<int>()))
            .ReturnsAsync(new MemoryStream([5, 6]));
        var service = CreateCanvasService(blob.Object, validator.Object);

        var result = await service.GetSnapshotPreviewAsync(canvas.Id, owner.Id);

        Assert.IsTrue(result.IsSuccess);
        validator.Verify(v => v.CreatePngThumbnailAsync(It.IsAny<Stream>(), It.IsAny<int>()), Times.Once);
    }

    [TestMethod]
    public async Task GetSnapshotPreview_FallbackCannotDecode_ReturnsUnexpected()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id, snapshotUrl: SnapshotUrl);
        var blob = CreateBlobMock();
        blob.Setup(b => b.DownloadPreviewAsync(canvas.Id)).ReturnsAsync((Stream?)null);
        blob.Setup(b => b.DownloadSnapshotAsync(canvas.Id)).ReturnsAsync(new MemoryStream([1, 2, 3, 4]));
        var validator = CreateImageValidatorMock();
        validator.Setup(v => v.CreatePngThumbnailAsync(It.IsAny<Stream>(), It.IsAny<int>()))
            .ReturnsAsync((Stream?)null);
        var service = CreateCanvasService(blob.Object, validator.Object);

        var result = await service.GetSnapshotPreviewAsync(canvas.Id, owner.Id);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Unexpected, result.ErrorType);
    }

    // ─── UploadSnapshotAsync ────────────────────────────────────

    [TestMethod]
    public async Task UploadSnapshot_CanvasNotFound_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.UploadSnapshotAsync(Guid.NewGuid(), owner.Id, BuildPng(), "image/png");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task UploadSnapshot_NonOwner_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var stranger = await SeedUserAsync("stranger");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.UploadSnapshotAsync(canvas.Id, stranger.Id, BuildPng(), "image/png");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
    }

    [TestMethod]
    public async Task UploadSnapshot_WrongContentType_ReturnsValidation()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var service = CreateCanvasService(CreateBlobMock().Object, CreateImageValidatorMock().Object);

        var result = await service.UploadSnapshotAsync(canvas.Id, owner.Id, BuildPng(), "image/jpeg");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task UploadSnapshot_HeaderPassesButDoesNotDecode_ReturnsValidation()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var validator = CreateImageValidatorMock();
        validator.Setup(v => v.IsDecodablePngAsync(It.IsAny<Stream>())).ReturnsAsync(false);
        var service = CreateCanvasService(CreateBlobMock().Object, validator.Object);

        var result = await service.UploadSnapshotAsync(canvas.Id, owner.Id, BuildPng(), "image/png");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        // A snapshot that never decoded must not have mutated the canvas row.
        var reloaded = await Context.Canvas.AsNoTracking().SingleAsync(c => c.Id == canvas.Id);
        Assert.IsNull(reloaded.SnapshotURL);
    }

    [TestMethod]
    public async Task UploadSnapshot_BlobReturnsEmptyUri_ReturnsUnexpected()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var validator = CreateImageValidatorMock();
        validator.Setup(v => v.IsDecodablePngAsync(It.IsAny<Stream>())).ReturnsAsync(true);
        var blob = CreateBlobMock();
        blob.Setup(b => b.UploadBlobAsync(canvas.Id, It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("");
        var service = CreateCanvasService(blob.Object, validator.Object);

        var result = await service.UploadSnapshotAsync(canvas.Id, owner.Id, BuildPng(), "image/png");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ErrorType.Unexpected, result.ErrorType);
    }

    [TestMethod]
    public async Task UploadSnapshot_ValidPng_StoresUrlAndStampsTakenAt()
    {
        var owner = await SeedUserAsync("owner");
        var canvas = await SeedCanvasAsync(owner.Id);
        var validator = CreateImageValidatorMock();
        validator.Setup(v => v.IsDecodablePngAsync(It.IsAny<Stream>())).ReturnsAsync(true);
        // Best-effort preview render returns nothing; the upload must still succeed.
        validator.Setup(v => v.CreatePngThumbnailAsync(It.IsAny<Stream>(), It.IsAny<int>()))
            .ReturnsAsync((Stream?)null);
        var blob = CreateBlobMock();
        blob.Setup(b => b.UploadBlobAsync(canvas.Id, It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(SnapshotUrl);
        var service = CreateCanvasService(blob.Object, validator.Object);

        var before = DateTimeOffset.UtcNow;
        var result = await service.UploadSnapshotAsync(canvas.Id, owner.Id, BuildPng(), "image/png");

        Assert.IsTrue(result.IsSuccess);
        var reloaded = await Context.Canvas.AsNoTracking().SingleAsync(c => c.Id == canvas.Id);
        Assert.AreEqual(SnapshotUrl, reloaded.SnapshotURL);
        Assert.IsTrue(reloaded.SnapshotTakenAt >= before);
    }
}
