using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Services;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Moq;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyServiceInviteByEmailTests
{
    private Mock<IPartyRepository> partyRepository = null!;
    private Mock<IPartyInviteRepository> partyInviteRepository = null!;
    private Mock<IBlockListRepository> blockListRepository = null!;
    private Mock<IUserRepository> userRepository = null!;
    private Mock<IPartyNotifier> partyNotifier = null!;
    private PartyService service = null!;
    private Party party = null!;
    private User invitedUser = null!;

    [TestInitialize]
    public void Initialize()
    {
        partyRepository = new Mock<IPartyRepository>();
        partyInviteRepository = new Mock<IPartyInviteRepository>();
        blockListRepository = new Mock<IBlockListRepository>();
        userRepository = new Mock<IUserRepository>();
        partyNotifier = new Mock<IPartyNotifier>();
        party = new Party { LeaderId = Guid.NewGuid() };
        invitedUser = new User { Email = "friend@example.com", UserName = "friend" };

        partyRepository.Setup(repository => repository.GetByIdAsync(party.Id)).ReturnsAsync(party);
        partyRepository.Setup(repository => repository.GetMemberCountAsync(party.Id)).ReturnsAsync(1);
        userRepository
            .Setup(repository => repository.FindByEmailAsync(invitedUser.Email))
            .ReturnsAsync(invitedUser);

        service = new PartyService(
            partyRepository.Object,
            partyInviteRepository.Object,
            blockListRepository.Object,
            partyNotifier.Object,
            new Mock<ICanvasService>().Object,
            new Mock<ICanvasRepository>().Object,
            userRepository.Object
        );
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenPartyDoesNotExist_ReturnsNotFound()
    {
        var missingPartyId = Guid.NewGuid();

        var result = await service.InviteUserByEmailAsync(missingPartyId, party.LeaderId, invitedUser.Email);

        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("Party not found.", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenCallerIsNotLeader_ReturnsForbidden()
    {
        var nonLeaderId = Guid.NewGuid();

        var result = await service.InviteUserByEmailAsync(party.Id, nonLeaderId, "unknown@example.com");

        Assert.AreEqual(ErrorType.Forbidden, result.ErrorType);
        Assert.AreEqual("Only the leader can invite people.", result.Error);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("not-an-email")]
    [DataRow("friend@")]
    [DataRow("@example.com")]
    [DataRow("friend@example.com\nother@example.com")]
    public async Task InviteUserByEmailAsync_WhenEmailIsInvalid_ReturnsValidationFailure(string? email)
    {
        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, email!);

        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("Enter a valid email address (up to 256 characters).", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenEmailExceeds256Characters_ReturnsValidationFailure()
    {
        var email = new string('a', 245) + "@example.com";

        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, email);

        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenEmailIsUnknown_ReturnsNotFound()
    {
        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, "unknown@example.com");

        Assert.AreEqual(ErrorType.NotFound, result.ErrorType);
        Assert.AreEqual("No user found with this email address.", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenEmailMatchesUser_ReturnsPendingInviteForThatUser()
    {
        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, invitedUser.Email);

        Assert.IsNotNull(result.Data);
        Assert.AreEqual(invitedUser.Id, result.Data.InvitedUserId);
        Assert.AreEqual(party.Id, result.Data.PartyId);
        Assert.AreEqual(party.LeaderId, result.Data.InvitedByUserId);
        Assert.AreEqual(InviteStatus.Pending, result.Data.InviteStatus);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenEmailHasSurroundingWhitespace_InvitesMatchingUser()
    {
        var email = $"  {invitedUser.Email}  ";

        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, email);

        Assert.IsNotNull(result.Data);
        Assert.AreEqual(invitedUser.Id, result.Data.InvitedUserId);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenEmailMatchesLeader_ReturnsValidationFailure()
    {
        invitedUser.Id = party.LeaderId;

        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, invitedUser.Email);

        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You cannot invite yourself.", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenUserIsAlreadyMember_ReturnsConflict()
    {
        partyRepository
            .Setup(repository => repository.IsUserInPartyAsync(party.Id, invitedUser.Id))
            .ReturnsAsync(true);

        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, invitedUser.Email);

        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("User is already in the party.", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenUserIsBlocked_ReturnsValidationFailure()
    {
        blockListRepository
            .Setup(repository => repository.IsBlockedAsync(party.LeaderId, invitedUser.Id))
            .ReturnsAsync(true);

        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, invitedUser.Email);

        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("You have blocked this user.", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenPartyIsFull_ReturnsValidationFailure()
    {
        partyRepository.Setup(repository => repository.GetMemberCountAsync(party.Id)).ReturnsAsync(5);

        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, invitedUser.Email);

        Assert.AreEqual(ErrorType.Validation, result.ErrorType);
        Assert.AreEqual("Party is full. (Max 5 members)", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenUserAlreadyHasPendingInvite_ReturnsConflict()
    {
        var pendingInvite = new PartyInvite { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) };
        partyInviteRepository
            .Setup(repository => repository.GetPendingInviteAsync(party.Id, invitedUser.Id))
            .ReturnsAsync(pendingInvite);

        var result = await service.InviteUserByEmailAsync(party.Id, party.LeaderId, invitedUser.Email);

        Assert.AreEqual(ErrorType.Conflict, result.ErrorType);
        Assert.AreEqual("An invite is already pending for this user.", result.Error);
    }

    [TestMethod]
    public async Task InviteUserByEmailAsync_WhenInviteIsSaved_NotifiesResolvedUserAfterSaveCompletes()
    {
        var saved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PartyInvite? deliveredInvite = null;
        partyInviteRepository
            .Setup(repository => repository.CreateInviteAsync(It.IsAny<PartyInvite>()))
            .Returns(saved.Task);
        partyNotifier
            .Setup(notifier => notifier.NotifyInvite(invitedUser.Id, It.IsAny<PartyInvite>()))
            .Callback<Guid, PartyInvite>((_, invite) => deliveredInvite = invite)
            .Returns(Task.CompletedTask);

        var invitation = service.InviteUserByEmailAsync(party.Id, party.LeaderId, invitedUser.Email);
        Assert.IsNull(deliveredInvite, "An unsaved invite must not be delivered.");
        saved.SetResult();
        var result = await invitation;

        Assert.IsNotNull(deliveredInvite);
        Assert.AreEqual(result.Data!.Id, deliveredInvite.Id);
        Assert.AreEqual(invitedUser.Id, deliveredInvite.InvitedUserId);
    }
}
