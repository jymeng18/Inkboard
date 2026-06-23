using Inkboard.Application.Auth.DTO;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class LogoutTests : TestBase
{
    [TestMethod]
    public async Task ValidToken_ReturnsSuccess()
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

        var result = await service.LogoutAsync(login.RefreshToken!);

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ValidToken_RevokesIt()
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

        var tokens = await context.RefreshTokens.ToListAsync();
        Assert.HasCount(1, tokens);
        Assert.IsTrue(tokens[0].IsRevoked);
    }

    [TestMethod]
    public async Task InvalidToken_StillReturnsSuccess()
    {
        var context = CreateDbContext();
        var service = CreateAuthService(context);

        var result = await service.LogoutAsync("non-existent-token");

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task DoubleLogout_IsIdempotent()
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
        var secondLogout = await service.LogoutAsync(login.RefreshToken!);

        Assert.IsTrue(secondLogout.Success);
    }
}
