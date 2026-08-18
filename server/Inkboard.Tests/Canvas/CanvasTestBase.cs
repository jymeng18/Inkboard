using System.Buffers.Binary;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Infra.Db;
using Moq;

namespace Inkboard.Tests.Canvases;

/// <summary>
/// Shared setup for the CanvasService rule tests. Uses the in-memory EF context
/// with real repositories (matching the existing service-test convention), and
/// exposes Moq doubles for the two genuinely external collaborators — blob
/// storage and image decoding — so the tests exercise the service's own rules,
/// not Azure or SkiaSharp.
/// </summary>
public abstract class CanvasTestBase : TestBase
{
    protected AppDbContext Context { get; private set; } = null!;

    [TestInitialize]
    public void BaseInitialize()
    {
        Context = CreateDbContext();
    }

    [TestCleanup]
    public void BaseCleanup()
    {
        Context.Dispose();
    }

    /// <summary>Service wired with real repos and no blob/image collaborators —
    /// enough for the operation, name, and force-end rules that never touch a blob.</summary>
    protected CanvasService CreateCanvasService()
    {
        return new CanvasService(
            new CanvasRepository(Context),
            new PartyRepository(Context),
            blobStorageService: null!,
            imageValidator: null!,
            new OperationRepository(Context)
        );
    }

    /// <summary>Service wired with the supplied blob/image doubles for the snapshot paths.</summary>
    protected CanvasService CreateCanvasService(
        IBlobStorageService blobStorageService,
        IImageValidator imageValidator
    )
    {
        return new CanvasService(
            new CanvasRepository(Context),
            new PartyRepository(Context),
            blobStorageService,
            imageValidator,
            new OperationRepository(Context)
        );
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

    protected async Task<Domain.Models.Canvas> SeedCanvasAsync(
        Guid ownerId,
        string name = "Test Canvas",
        string? snapshotUrl = null
    )
    {
        var canvas = new Domain.Models.Canvas
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

    /// <summary>Seeds a party bound to <paramref name="canvasId"/> with the given
    /// leader and member, so the member has an active party whose canvas link the
    /// access rules can be checked against.</summary>
    protected async Task<Party> SeedPartyWithMemberAsync(
        Guid leaderId,
        Guid memberId,
        Guid? canvasId
    )
    {
        var party = new Party
        {
            LeaderId = leaderId,
            CanvasId = canvasId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        Context.Parties.Add(party);
        Context.PartyMembers.Add(new PartyMember
        {
            PartyId = party.Id,
            UserId = leaderId,
            Role = UserRole.Leader,
        });
        Context.PartyMembers.Add(new PartyMember
        {
            PartyId = party.Id,
            UserId = memberId,
            Role = UserRole.Member,
        });
        await Context.SaveChangesAsync();
        return party;
    }

    protected async Task<CanvasOperation> SeedOperationAsync(
        Guid canvasId,
        Guid userId,
        string data,
        ActionType type = ActionType.Draw,
        DateTimeOffset? timestamp = null
    )
    {
        var op = new CanvasOperation
        {
            CanvasId = canvasId,
            UserId = userId,
            OperationData = data,
            Type = type,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
        };
        Context.CanvasOperations.Add(op);
        await Context.SaveChangesAsync();
        return op;
    }

    /// <summary>Builds a byte payload whose first 24 bytes are a well-formed PNG
    /// signature + IHDR carrying the given dimensions, padded to <paramref name="totalBytes"/>.
    /// This is exactly what CanvasService.ImageDataValidator inspects.</summary>
    protected static MemoryStream BuildPng(uint width = 100, uint height = 100, int totalBytes = 2048)
    {
        var buffer = new byte[Math.Max(totalBytes, 24)];

        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(buffer, 0);

        // IHDR chunk length (13) + type "IHDR"
        buffer[8] = 0x00;
        buffer[9] = 0x00;
        buffer[10] = 0x00;
        buffer[11] = 0x0D;
        buffer[12] = (byte)'I';
        buffer[13] = (byte)'H';
        buffer[14] = (byte)'D';
        buffer[15] = (byte)'R';

        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(16, 4), width);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(20, 4), height);

        return new MemoryStream(buffer) { Position = 0 };
    }

    protected static Mock<IBlobStorageService> CreateBlobMock() =>
        new(MockBehavior.Strict);

    protected static Mock<IImageValidator> CreateImageValidatorMock() =>
        new(MockBehavior.Strict);
}
