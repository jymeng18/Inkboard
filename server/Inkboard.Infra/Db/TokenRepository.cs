using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class TokenRepository : ITokenRepository
{
    private readonly AppDbContext _context;

    public TokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExpiredAsync()
    {
        await _context.RefreshTokens.Where(t => t.ExpiresAt < DateTime.UtcNow).ExecuteDeleteAsync();
    }

    public async Task<RefreshToken?> FindByTokenHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context
            .RefreshTokens.Where(t =>
                t.UserId == userId && !t.IsRevoked && t.ExpiresAt < DateTime.UtcNow
            )
            .ToListAsync();
    }

    public async Task RevokeAsync(RefreshToken refreshToken)
    {
        refreshToken.IsRevoked = true;
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }
}
