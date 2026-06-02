using System.Security.Claims;
using System.Text;
using Inkboard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Inkboard.Infra.Auth;

public class TokenGenerator : ITokenGenerator
{
    private readonly IConfiguration Configuration;

    public TokenGenerator(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public string GenerateToken(Guid userId, string email)
    {
        var tokenHandler = new JsonWebTokenHandler();

        // Pull the Key out of your appsettings.json
        var secretKey = Configuration["JwtConfig:Jwt:Key"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is missing from configuration.");
        }

        var key = Encoding.UTF8.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60),
            Issuer = Configuration["JwtConfig:Issuer"],
            Audience = Configuration["JwtConfig:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return token;
    }
}
