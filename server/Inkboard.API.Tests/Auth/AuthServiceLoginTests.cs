using Inkboard.Application.Auth.DTO;
using Inkboard.Application.Services;
using Inkboard.Infra.Db;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Inkboard.API.Tests.Auth;

[TestClass]
public sealed class LoginTests : TestBase
{
    private async Task<AuthService> RegisterAndGetService(AppDbContext context)
    {
        var service = CreateAuthService(context);
        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });
        return service;
    }

    [TestMethod]
    public async Task ValidCredentials_ReturnsSuccessWithTokens()
    {
        var context = CreateDbContext();
        var service = await RegisterAndGetService(context);

        var result = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AccessToken);
        Assert.IsNotNull(result.RefreshToken);
    }

    [TestMethod]
    public async Task ValidCredentials_AccessTokenContainsUserId()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var regResult = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var result = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(regResult.UserId);
        StringAssert.Contains(result.AccessToken!, regResult.UserId.ToString()!);
    }

    [TestMethod]
    public async Task WrongPassword_ReturnsError()
    {
        var context = CreateDbContext();
        var service = await RegisterAndGetService(context);

        var result = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = "wrongpassword",
        });

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid email or password.", result.ErrorMessage);
        Assert.IsNull(result.AccessToken);
    }

    [TestMethod]
    public async Task NonExistentEmail_ReturnsError()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.LoginAsync(new LoginRequestModel
        {
            Email = "nonexistent@example.com",
            Password = ValidPassword,
        });

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid email or password.", result.ErrorMessage);
        Assert.IsNull(result.AccessToken);
    }

    [TestMethod]
    public async Task ErrorMessage_DoesNotRevealWhichFieldIsWrong()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var wrongEmailResult = await service.LoginAsync(new LoginRequestModel
        {
            Email = "wrong@example.com",
            Password = ValidPassword,
        });

        await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var wrongPassResult = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = "wrongpassword",
        });

        Assert.AreEqual(wrongEmailResult.ErrorMessage, wrongPassResult.ErrorMessage);
    }

    [TestMethod]
    public async Task ValidCredentials_PersistsRefreshToken()
    {
        var context = CreateDbContext();
        var service = await RegisterAndGetService(context);

        var result = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsTrue(result.Success);

        var tokens = await context.RefreshTokens.ToListAsync();
        Assert.HasCount(1, tokens);
        Assert.IsFalse(tokens[0].IsRevoked);
        Assert.IsNotNull(tokens[0].TokenHash);
        Assert.AreNotEqual(result.RefreshToken, tokens[0].TokenHash);
    }

    [TestMethod]
    public async Task ValidCredentials_CallsGenerateTokenWithCorrectParams()
    {
        var context = CreateDbContext();
        var tokenGenMock = CreateTokenGeneratorMock();
        tokenGenMock
            .Setup(t => t.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns((Guid userId, string email) => $"token-{userId}-{email}")
            .Verifiable();
        var service = CreateAuthService(context, tokenGenMock);

        var regResult = await service.RegisterAsync(new RegisterRequestModel
        {
            UserName = ValidUserName,
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var result = await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(regResult.UserId);
        Assert.AreEqual($"token-{regResult.UserId}-{ValidEmail}", result.AccessToken);
        tokenGenMock.Verify(
            t => t.GenerateToken(regResult.UserId.Value, ValidEmail), Times.Once);
    }

    [TestMethod]
    public async Task ValidCredentials_CallsGenerateRefreshToken()
    {
        var context = CreateDbContext();
        var tokenGenMock = CreateTokenGeneratorMock();
        var service = CreateAuthService(context, tokenGenMock);

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

        tokenGenMock.Verify(t => t.GenerateRefreshToken(), Times.Once);
    }

    [TestMethod]
    public async Task RefreshTokenExpiry_IsSevenDays()
    {
        var context = CreateDbContext();
        var service = await RegisterAndGetService(context);

        await service.LoginAsync(new LoginRequestModel
        {
            Email = ValidEmail,
            Password = ValidPassword,
        });

        var tokens = await context.RefreshTokens.ToListAsync();
        Assert.HasCount(1, tokens);
        var expectedExpiry = DateTime.UtcNow.AddDays(7);
        Assert.IsTrue(tokens[0].ExpiresAt <= expectedExpiry.AddMinutes(1));
        Assert.IsTrue(tokens[0].ExpiresAt >= expectedExpiry.AddMinutes(-1));
    }
}
