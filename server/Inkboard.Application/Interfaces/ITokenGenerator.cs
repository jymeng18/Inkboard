namespace Inkboard.Application.Interfaces;

public interface ITokenGenerator
{
    string GenerateToken(Guid userId, string email, string userName);
    string GenerateRefreshToken();
}
