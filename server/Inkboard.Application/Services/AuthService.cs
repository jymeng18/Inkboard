using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services;

public class AuthService : IAuthService
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUserRepository _repository;
    private readonly IValidator<RegisterRequestModel> _registerValidator;
    private readonly IValidator<LoginRequestModel> _loginValidator;
    private readonly ITokenRepository _tokenRepository;

    public AuthService(
        ITokenGenerator tokenGenerator,
        IUserRepository repository,
        IValidator<RegisterRequestModel> registerRequestValidator,
        IValidator<LoginRequestModel> loginRequestValidator,
        ITokenRepository tokenRepository
    )
    {
        _tokenGenerator = tokenGenerator;
        _repository = repository;
        _registerValidator = registerRequestValidator;
        _loginValidator = loginRequestValidator;
        _tokenRepository = tokenRepository;
    }

    public async Task<LoginResult> LoginAsync(LoginRequestModel request)
    {
        var result = await _loginValidator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result
                .Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return new LoginResult { ValidationErrors = errors };
        }

        var user = await _repository.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new LoginResult { ErrorMessage = "Invalid email or password." };
        }

        // compare hashpw <-> pw
        bool validPassword = BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            return new LoginResult { ErrorMessage = "Invalid email or password." };
        }

        var token = _tokenGenerator.GenerateToken(user.Id, request.Email); // access token

        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken))
        );

        var refreshToken = new RefreshToken
        {
            TokenHash = tokenHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        await _tokenRepository.CreateAsync(refreshToken);

        return new LoginResult
        {
            Success = true,
            AccessToken = token,
            RefreshToken = rawRefreshToken,
        };
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequestModel request)
    {
        // Authorize all fields in request
        var result = await _registerValidator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result
                .Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return new RegisterResult { ValidationErrors = errors };
        }

        bool emailExists = await _repository.EmailExistsAsync(request.Email);
        if (emailExists)
        {
            return new RegisterResult { ErrorMessage = "Email is already registered." };
        }

        // Create new user to save to db(Users)
        User user = new()
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password, 12), // workfactor = 12
        };

        try
        {
            await _repository.CreateUserAsync(user);
            return new RegisterResult { Success = true, UserId = user.Id };
        }
        catch (Exception)
        {
            return new RegisterResult { ErrorMessage = "An unexpected error occured." };
        }
    }

    public async Task<LogoutResult> LogoutAsync(string rawRefreshToken)
    {
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken))
        );

        var storedToken = await _tokenRepository.FindByTokenHashAsync(tokenHash);
        if (storedToken is not null)
        {
            await _tokenRepository.RevokeAsync(storedToken);
        }

        return new LogoutResult { Success = true };
    }

    public async Task<LoginResult> RefreshAsync(string rawRefreshToken)
    {
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken))
        );

        var storedToken = await _tokenRepository.FindByTokenHashAsync(tokenHash);
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return new LoginResult { ErrorMessage = "Invalid or expired refresh token." };
        }

        // Rotation: revoke this token and all other active tokens for the user
        var activeTokens = await _tokenRepository.GetActiveByUserIdAsync(storedToken.UserId);
        foreach (var t in activeTokens)
        {
            await _tokenRepository.RevokeAsync(t);
        }

        var user = await _repository.GetByIdAsync(storedToken.UserId);
        if (user is null)
        {
            return new LoginResult { ErrorMessage = "User not found." };
        }

        var newAccessToken = _tokenGenerator.GenerateToken(user.Id, user.Email);

        var newRawRefresh = _tokenGenerator.GenerateRefreshToken();
        var newHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(newRawRefresh))
        );

        var newRefreshToken = new RefreshToken
        {
            TokenHash = newHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        await _tokenRepository.CreateAsync(newRefreshToken);

        return new LoginResult
        {
            Success = true,
            AccessToken = newAccessToken,
            RefreshToken = newRawRefresh,
        };
    }
}
