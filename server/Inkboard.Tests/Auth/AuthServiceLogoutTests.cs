using Microsoft.EntityFrameworkCore;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class LogoutTests : AuthTestBase
{
    [TestMethod]
    public async Task ValidToken_ReturnsSuccess()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        var result = await Service.LogoutAsync(login.RefreshToken!);

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ValidToken_RevokesIt()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        await Service.LogoutAsync(login.RefreshToken!);

        var tokens = await Context.RefreshTokens.ToListAsync();
        Assert.HasCount(1, tokens);
        Assert.IsTrue(tokens[0].IsRevoked);
    }

    [TestMethod]
    public async Task InvalidToken_StillReturnsSuccess()
    {
        var result = await Service.LogoutAsync("non-existent-token");

        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task DoubleLogout_IsIdempotent()
    {
        await RegisterDefaultAsync();
        var login = await LoginDefaultAsync();

        await Service.LogoutAsync(login.RefreshToken!);
        var secondLogout = await Service.LogoutAsync(login.RefreshToken!);

        Assert.IsTrue(secondLogout.Success);
    }
}
