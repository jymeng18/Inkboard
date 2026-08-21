using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class RefreshTests : AuthTestBase
{
    [TestMethod]
    public async Task ValidToken_ReturnsNewTokens()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        var result = await Service.RefreshAsync(login.RefreshToken!);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AccessToken);
        Assert.IsNotNull(result.RefreshToken);
    }

    [TestMethod]
    public async Task ValidToken_ReturnsDifferentAccessToken()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        var result = await Service.RefreshAsync(login.RefreshToken!);

        Assert.AreNotEqual(login.AccessToken, result.AccessToken);
    }

    [TestMethod]
    public async Task ValidToken_ReturnsDifferentRefreshToken()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        var result = await Service.RefreshAsync(login.RefreshToken!);

        Assert.AreNotEqual(login.RefreshToken, result.RefreshToken);
    }

    [TestMethod]
    public async Task ValidToken_RevokesOldToken()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        var oldHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(login.RefreshToken!))
        );

        await Service.RefreshAsync(login.RefreshToken!);

        var oldToken = await Context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == oldHash);
        Assert.IsNotNull(oldToken);
        Assert.IsTrue(oldToken.IsRevoked);
    }

    [TestMethod]
    public async Task ValidToken_IssuesNewRefreshTokenInDb()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        await Service.RefreshAsync(login.RefreshToken!);

        var tokens = await Context.RefreshTokens.ToListAsync();
        Assert.HasCount(2, tokens);
        Assert.HasCount(1, tokens.Where(t => !t.IsRevoked));
    }

    [TestMethod]
    public async Task RevokesAllActiveTokensForUser()
    {
        await RegisterDefaultAsync();

        await LoginDefaultAsync();
        var login2 = await LoginDefaultAsync();

        await Service.RefreshAsync(login2.RefreshToken!);

        var tokens = await Context.RefreshTokens.ToListAsync();
        Assert.HasCount(3, tokens);
        Assert.HasCount(1, tokens.Where(t => !t.IsRevoked));
    }

    [TestMethod]
    public async Task NonExistentToken_ReturnsError()
    {
        var result = await Service.RefreshAsync("non-existent-token");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid or expired refresh token.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task RevokedToken_ReturnsError()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        await Service.LogoutAsync(login.RefreshToken!);
        var result = await Service.RefreshAsync(login.RefreshToken!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid or expired refresh token.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ExpiredToken_ReturnsError()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        var rawHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(login.RefreshToken!))
        );
        var stored = await Context.RefreshTokens.FirstAsync(t => t.TokenHash == rawHash);
        stored.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        await Context.SaveChangesAsync();

        var result = await Service.RefreshAsync(login.RefreshToken!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid or expired refresh token.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task TokenForDeletedUser_ReturnsError()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        var rawHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(login.RefreshToken!))
        );
        var stored = await Context.RefreshTokens.FirstAsync(t => t.TokenHash == rawHash);
        stored.UserId = Guid.NewGuid(); // point to non-existent user
        await Context.SaveChangesAsync();

        var result = await Service.RefreshAsync(login.RefreshToken!);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("User not found.", result.ErrorMessage);
    }
}
