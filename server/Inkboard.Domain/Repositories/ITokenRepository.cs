using Inkboard.Domain.Models;

namespace Inkboard.Domain.Repositories;

public interface ITokenRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash);
    
    // Get users that have valid refresh tokens
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
    Task CreateAsync(RefreshToken refreshToken);
    Task RevokeAsync(RefreshToken refreshToken);
    Task DeleteExpiredAsync();
}