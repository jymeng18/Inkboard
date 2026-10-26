#nullable enable

using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Common;
using Inkboard.Application.Users.DTO;

namespace Inkboard.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequestModel request);
    Task<RegisterResult> RegisterAsync(RegisterRequestModel request);
    Task<LogoutResult> LogoutAsync(string refreshToken);
    Task<LoginResult> RefreshAsync(string refreshToken);
    Task<Result<AccountDto>> GetAccountAsync(Guid userId);
    Task<Result> DeleteAccountAsync(Guid userId, string password);
}

public class LoginResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? ErrorMessage { get; init; }
    public IDictionary<string, string[]>? ValidationErrors { get; init; }
}

public class RegisterResult
{
    public bool Success { get; init; }
    public Guid? UserId { get; init; }
    public string? ErrorMessage { get; init; }
    public IDictionary<string, string[]>? ValidationErrors { get; init; }
}

public class LogoutResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
