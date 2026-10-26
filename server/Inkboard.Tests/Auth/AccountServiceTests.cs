using FluentValidation;
using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class AccountServiceTests
{
    private const string Password = " AccountPass123 ";
    private static readonly string PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(Password, 4);
    private Mock<IUserRepository> userRepository = null!;
    private Mock<IPartyRepository> partyRepository = null!;
    private Mock<IPartyService> partyService = null!;
    private Mock<ICanvasRepository> canvasRepository = null!;
    private Mock<IBlobStorageService> blobStorage = null!;
    private Mock<IConnectionStore> connectionStore = null!;
    private AuthService service = null!;
    private User user = null!;
    private User? storedUser;

    [TestInitialize]
    public void Initialize()
    {
        user = new User { UserName = "artist", Email = "artist@example.com", PasswordHash = PasswordHash };
        storedUser = user;
        userRepository = new Mock<IUserRepository>();
        partyRepository = new Mock<IPartyRepository>();
        partyService = new Mock<IPartyService>();
        canvasRepository = new Mock<ICanvasRepository>();
        blobStorage = new Mock<IBlobStorageService>();
        connectionStore = new Mock<IConnectionStore>();
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id)).ReturnsAsync(() => storedUser);
        userRepository.Setup(repository => repository.DeleteUserAsync(user))
            .Callback(() => storedUser = null).Returns(Task.CompletedTask);
        canvasRepository.Setup(repository => repository.GetCanvasesByUserIdAsync(user.Id))
            .ReturnsAsync(new List<Canvas>());

        service = new AuthService(
            new Mock<ITokenGenerator>().Object,
            userRepository.Object,
            new Mock<IValidator<RegisterRequestModel>>().Object,
            new Mock<IValidator<LoginRequestModel>>().Object,
            new Mock<ITokenRepository>().Object,
            partyRepository.Object,
            partyService.Object,
            canvasRepository.Object,
            blobStorage.Object,
            connectionStore.Object,
            NullLogger<AuthService>.Instance
        );
    }

    [TestMethod]
    public async Task GetAccountAsync_WhenAccountExists_ReturnsPublicAccountInformation()
    {
        var result = await service.GetAccountAsync(user.Id);

        Assert.IsNotNull(result.Data);
        Assert.AreEqual(user.Id, result.Data.Id);
        Assert.AreEqual(user.UserName, result.Data.UserName);
        Assert.AreEqual(user.Email, result.Data.Email);
        Assert.AreEqual(user.CreatedAt, result.Data.CreatedAt);
    }

    [TestMethod]
    public async Task GetAccountAsync_WhenAccountDoesNotExist_ReturnsNotFound()
    {
        storedUser = null;

        var result = await service.GetAccountAsync(user.Id);

        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task DeleteAccountAsync_WhenPasswordIsMissing_PreservesAccount(string? password)
    {
        var result = await service.DeleteAccountAsync(user.Id, password!);

        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.IsNotNull(storedUser);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenPasswordExceeds128Characters_PreservesAccount()
    {
        var password = new string('a', 129);

        var result = await service.DeleteAccountAsync(user.Id, password);

        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.IsNotNull(storedUser);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenAccountDoesNotExist_ReturnsNotFound()
    {
        storedUser = null;

        var result = await service.DeleteAccountAsync(user.Id, Password);

        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenPasswordIsIncorrect_PreservesAccount()
    {
        var result = await service.DeleteAccountAsync(user.Id, "incorrect-password");

        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.IsNotNull(storedUser);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenPasswordIsIncorrect_DoesNotLeaveParty()
    {
        var leftParty = false;
        var party = new Party { LeaderId = user.Id };
        partyRepository.Setup(repository => repository.GetActivePartyForUserAsync(user.Id)).ReturnsAsync(party);
        partyService.Setup(parties => parties.LeavePartyAsync(party.Id, user.Id))
            .Callback(() => leftParty = true).ReturnsAsync(Result.Ok());

        await service.DeleteAccountAsync(user.Id, "incorrect-password");

        Assert.IsFalse(leftParty);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenPasswordMatchesExactly_RemovesAccount()
    {
        // Leading/trailing spaces belong to the password and must not be trimmed.
        await service.DeleteAccountAsync(user.Id, Password);

        var account = await service.GetAccountAsync(user.Id);
        Assert.AreEqual(ErrorType.NotFound, account.ErrorType);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenUserHasParty_LeavesPartyBeforeRemovingAccount()
    {
        var party = new Party { LeaderId = user.Id };
        var accountExistedWhenLeaving = false;
        partyRepository.Setup(repository => repository.GetActivePartyForUserAsync(user.Id)).ReturnsAsync(party);
        partyService.Setup(parties => parties.LeavePartyAsync(party.Id, user.Id))
            .Callback(() => accountExistedWhenLeaving = storedUser is not null).ReturnsAsync(Result.Ok());

        await service.DeleteAccountAsync(user.Id, Password);

        Assert.IsTrue(accountExistedWhenLeaving);
        Assert.IsNull(storedUser);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenLeavingPartyFails_PreservesAccount()
    {
        var party = new Party { LeaderId = user.Id };
        partyRepository.Setup(repository => repository.GetActivePartyForUserAsync(user.Id)).ReturnsAsync(party);
        partyService.Setup(parties => parties.LeavePartyAsync(party.Id, user.Id))
            .ReturnsAsync(Result.Fail(ErrorType.Conflict, "Party changed. Try again."));

        var result = await service.DeleteAccountAsync(user.Id, Password);

        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.IsNotNull(storedUser);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenDatabaseCommitIsPending_KeepsLiveSessionsConnected()
    {
        var committed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disconnected = false;
        userRepository.Setup(repository => repository.DeleteUserAsync(user)).Returns(committed.Task);
        connectionStore.Setup(connections => connections.DisconnectUserAsync(user.Id))
            .Callback(() => disconnected = true).Returns(Task.CompletedTask);

        var deletion = service.DeleteAccountAsync(user.Id, Password);
        Assert.IsFalse(disconnected);
        committed.SetResult();
        await deletion;

        Assert.IsTrue(disconnected);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenOwnedCanvasHasSnapshot_RemovesItsStoredImagesAfterAccountDeletion()
    {
        var canvas = new Canvas { OwnerId = user.Id, SnapshotURL = "https://storage.test/snapshot.png" };
        var cleanedUpAfterDeletion = false;
        canvasRepository.Setup(repository => repository.GetCanvasesByUserIdAsync(user.Id))
            .ReturnsAsync(new List<Canvas> { canvas });
        blobStorage.Setup(storage => storage.DeleteCanvasBlobsAsync(canvas.Id))
            .Callback(() => cleanedUpAfterDeletion = storedUser is null).Returns(Task.CompletedTask);

        await service.DeleteAccountAsync(user.Id, Password);

        Assert.IsTrue(cleanedUpAfterDeletion);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenBlobCleanupFails_StillReportsCommittedDeletion()
    {
        var canvas = new Canvas { OwnerId = user.Id, SnapshotURL = "https://storage.test/snapshot.png" };
        canvasRepository.Setup(repository => repository.GetCanvasesByUserIdAsync(user.Id))
            .ReturnsAsync(new List<Canvas> { canvas });
        blobStorage.Setup(storage => storage.DeleteCanvasBlobsAsync(canvas.Id))
            .ThrowsAsync(new IOException("Storage unavailable."));

        var result = await service.DeleteAccountAsync(user.Id, Password);

        Assert.IsNull(storedUser);
        Assert.IsTrue(result.IsSuccess, "Blob cleanup cannot turn a committed account deletion into a failed one.");
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenDatabaseDeletionFails_PreservesStoredSnapshots()
    {
        var snapshotRemoved = false;
        var canvas = new Canvas { OwnerId = user.Id, SnapshotURL = "https://storage.test/snapshot.png" };
        canvasRepository.Setup(repository => repository.GetCanvasesByUserIdAsync(user.Id))
            .ReturnsAsync(new List<Canvas> { canvas });
        userRepository.Setup(repository => repository.DeleteUserAsync(user)).ThrowsAsync(new IOException("Database unavailable."));
        blobStorage.Setup(storage => storage.DeleteCanvasBlobsAsync(canvas.Id))
            .Callback(() => snapshotRemoved = true).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<IOException>(() => service.DeleteAccountAsync(user.Id, Password));

        Assert.IsFalse(snapshotRemoved);
    }
}
