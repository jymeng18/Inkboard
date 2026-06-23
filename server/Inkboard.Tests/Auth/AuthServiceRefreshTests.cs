using System.Security.Cryptography;
using System.Text;
using Inkboard.Application.Auth.DTO;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class RefreshTests : TestBase
{
    [TestMethod]
    public async Task ValidToken_ReturnsNewTokens()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var result = await service.RefreshAsync(login.RefreshToken!);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AccessToken);
        Assert.IsNotNull(result.RefreshToken);
    }

    [TestMethod]
    public async Task ValidToken_ReturnsDifferentAccessToken()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var result = await service.RefreshAsync(login.RefreshToken!);

        Assert.AreNotEqual(login.AccessToken, result.AccessToken);
    }

    [TestMethod]
    public async Task ValidToken_ReturnsDifferentRefreshToken()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var result = await service.RefreshAsync(login.RefreshToken!);

        Assert.AreNotEqual(login.RefreshToken, result.RefreshToken);
    }

    [TestMethod]
    public async Task ValidToken_RevokesOldToken()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var oldHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(login.RefreshToken!))
        );

        await service.RefreshAsync(login.RefreshToken!);

        var oldToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == oldHash);
        Assert.IsNotNull(oldToken);
        Assert.IsTrue(oldToken.IsRevoked);
    }

    [TestMethod]
    public async Task ValidToken_IssuesNewRefreshTokenInDb()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        await service.RefreshAsync(login.RefreshToken!);

        var tokens = await context.RefreshTokens.ToListAsync();
        Assert.HasCount(2, tokens);
        Assert.HasCount(1, tokens.Where(t => !t.IsRevoked));
    }

    [TestMethod]
    public async Task RevokesAllActiveTokensForUser()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });
        var login2 = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        await service.RefreshAsync(login2.RefreshToken!);

        var tokens = await context.RefreshTokens.ToListAsync();
        Assert.HasCount(3, tokens);
        Assert.HasCount(1, tokens.Where(t => !t.IsRevoked));
    }

    [TestMethod]
    public async Task NonExistentToken_ReturnsError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.RefreshAsync("non-existent-token");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid or expired refresh token.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task RevokedToken_ReturnsError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        await service.LogoutAsync(login.RefreshToken!);
        var result = await service.RefreshAsync(login.RefreshToken!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid or expired refresh token.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExpiredToken_ReturnsError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var rawHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(login.RefreshToken!))
        );
        var stored = await context.RefreshTokens
            .FirstAsync(t => t.TokenHash == rawHash);
        stored.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var result = await service.RefreshAsync(login.RefreshToken!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid or expired refresh token.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task TokenForDeletedUser_ReturnsError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var login = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var rawHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(login.RefreshToken!))
        );
        var stored = await context.RefreshTokens
            .FirstAsync(t => t.TokenHash == rawHash);
        stored.UserId = Guid.NewGuid();  // point to non-existent user
        await context.SaveChangesAsync();

        var result = await service.RefreshAsync(login.RefreshToken!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("User not found.", result.ErrorMessage);
    }
}
