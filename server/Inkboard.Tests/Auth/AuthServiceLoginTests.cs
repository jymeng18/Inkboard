using Inkboard.Application.Auth.DTO;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class LoginTests : AuthTestBase
{
    [TestMethod]
    public async Task ValidCredentials_ReturnsSuccessWithTokens()
    {
        await RegisterDefaultAsync();

        var result = await LoginDefaultAsync();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AccessToken);
        Assert.IsNotNull(result.RefreshToken);
    }

    [TestMethod]
    public async Task ValidCredentials_AccessTokenContainsUserId()
    {
        var regResult = await RegisterDefaultAsync();

        var result = await LoginDefaultAsync();

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(regResult.UserId);
        Assert.Contains(regResult.UserId.ToString()!, result.AccessToken!);
    }

    [TestMethod]
    public async Task WrongPassword_ReturnsError()
    {
        await RegisterDefaultAsync();

        var result = await Service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = "wrongpassword" }
        );

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid email or password.", result.ErrorMessage);
        Assert.IsNull(result.AccessToken);
    }

    [TestMethod]
    public async Task NonExistentEmail_ReturnsError()
    {
        var result = await Service.LoginAsync(
            new LoginRequestModel { Email = "nonexistent@example.com", Password = ValidPassword }
        );

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid email or password.", result.ErrorMessage);
        Assert.IsNull(result.AccessToken);
    }

    [TestMethod]
    public async Task ErrorMessage_DoesNotRevealWhichFieldIsWrong()
    {
        var wrongEmailResult = await Service.LoginAsync(
            new LoginRequestModel { Email = "wrong@example.com", Password = ValidPassword }
        );

        await RegisterDefaultAsync();

        var wrongPassResult = await Service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = "wrongpassword" }
        );

        Assert.AreEqual(wrongEmailResult.ErrorMessage, wrongPassResult.ErrorMessage);
    }

    [TestMethod]
    public async Task ValidCredentials_PersistsRefreshToken()
    {
        await RegisterDefaultAsync();

        var result = await LoginDefaultAsync();

        Assert.IsTrue(result.Success);

        var tokens = await Context.RefreshTokens.ToListAsync();
        Assert.HasCount(1, tokens);
        Assert.IsFalse(tokens[0].IsRevoked);
        Assert.IsNotNull(tokens[0].TokenHash);
        Assert.AreNotEqual(result.RefreshToken, tokens[0].TokenHash);
    }

    [TestMethod]
    public async Task ValidCredentials_CallsGenerateTokenWithCorrectParams()
    {
        var tokenGenMock = CreateTokenGeneratorMock();
        tokenGenMock
            .Setup(t => t.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((Guid userId, string email, string _) => $"token-{userId}-{email}")
            .Verifiable();
        var service = CreateAuthService(Context, tokenGenMock);

        var regResult = await service.RegisterAsync(NewRegisterRequest());

        var result = await service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = ValidPassword }
        );

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(regResult.UserId);
        Assert.AreEqual($"token-{regResult.UserId}-{ValidEmail}", result.AccessToken);
        tokenGenMock.Verify(
            t => t.GenerateToken(regResult.UserId.Value, ValidEmail, It.IsAny<string>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task ValidCredentials_CallsGenerateRefreshToken()
    {
        var tokenGenMock = CreateTokenGeneratorMock();
        var service = CreateAuthService(Context, tokenGenMock);

        await service.RegisterAsync(NewRegisterRequest());

        await service.LoginAsync(
            new LoginRequestModel { Email = ValidEmail, Password = ValidPassword }
        );

        tokenGenMock.Verify(t => t.GenerateRefreshToken(), Times.Once);
    }

    [TestMethod]
    public async Task RefreshTokenExpiry_IsSevenDays()
    {
        await RegisterDefaultAsync();

        await LoginDefaultAsync();

        var tokens = await Context.RefreshTokens.ToListAsync();
        Assert.HasCount(1, tokens);
        var expectedExpiry = DateTimeOffset.UtcNow.AddDays(7);
        Assert.IsTrue(tokens[0].ExpiresAt <= expectedExpiry.AddMinutes(1));
        Assert.IsTrue(tokens[0].ExpiresAt >= expectedExpiry.AddMinutes(-1));
    }
}
