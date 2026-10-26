using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Users.DTO;
using Inkboard.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Inkboard.Application.Services;

public partial class AuthService
{
    private readonly IPartyRepository _partyRepository;
    private readonly IPartyService _partyService;
    private readonly ICanvasRepository _canvasRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConnectionStore _connectionStore;
    private readonly ILogger<AuthService> _logger;

    private AuthService(
        IPartyRepository partyRepository,
        IPartyService partyService,
        ICanvasRepository canvasRepository,
        IBlobStorageService blobStorageService,
        IConnectionStore connectionStore,
        ILogger<AuthService> logger
    )
    {
        _partyRepository = partyRepository;
        _partyService = partyService;
        _canvasRepository = canvasRepository;
        _blobStorageService = blobStorageService;
        _connectionStore = connectionStore;
        _logger = logger;
    }

    public async Task<Result<AccountDto>> GetAccountAsync(Guid userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        if (user is null)
            return Result<AccountDto>.Fail(ErrorType.NotFound, "Account not found.");

        return Result<AccountDto>.Ok(
            new AccountDto(user.Id, user.UserName, user.Email, user.CreatedAt)
        );
    }

    public async Task<Result> DeleteAccountAsync(Guid userId, string password)
    {
        const int pwMaxLength = 128;
        if (string.IsNullOrEmpty(password) || password.Length > pwMaxLength)
        {
            return Result.Fail(ErrorType.Validation, "Enter your current password.");
        }

        var user = await _repository.GetByIdAsync(userId);
        if (user is null)
            return Result.Fail(ErrorType.NotFound, "Account not found.");

        if (!BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
        {
            return Result.Fail(ErrorType.Forbidden, "That password is incorrect.");
        }

        var party = await _partyRepository.GetActivePartyForUserAsync(userId);
        if (party is not null)
        {
            var leaveResult = await _partyService.LeavePartyAsync(party.Id, userId);
            if (!leaveResult.IsSuccess)
                return Result.Fail(leaveResult.ErrorType, leaveResult.Error);
        }

        var canvases = await _canvasRepository.GetCanvasesByUserIdAsync(userId);
        await _repository.DeleteUserAsync(user);

        // The database commit removes refresh tokens and owned canvases together.
        // Disconnect every live tab only after that commit has succeeded.
        try
        {
            await _connectionStore.DisconnectUserAsync(userId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to notify disconnected sessions for deleted account {UserId}.",
                userId
            );
        }

        foreach (var canvas in canvases)
        {
            if (string.IsNullOrEmpty(canvas.SnapshotURL))
                continue;

            try
            {
                await _blobStorageService.DeleteCanvasBlobsAsync(canvas.Id);
            }
            catch (Exception exception)
            {
                // The account is already deleted. Do not report a failed deletion
                // and encourage a retry with credentials that no longer exist.
                // TODO: persist failed blob cleanup jobs for automatic retries.
                _logger.LogError(
                    exception,
                    "Failed to clean up blobs for deleted canvas {CanvasId}.",
                    canvas.Id
                );
            }
        }

        return Result.Ok();
    }
}
