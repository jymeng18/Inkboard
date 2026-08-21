using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class AuthEndpointTests : IntegrationTestBase
{
    private const string Email = "endpoint@test.com";
    private const string Password = "TestPass123";
    private const string UserName = "endpointuser";

    [TestMethod]
    public async Task Register_CreatesUser_Returns201WithId()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = Email,
                password = Password,
                userName = UserName,
            }
        );

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadJsonAsync(response);
        Assert.IsTrue(body.TryGetProperty("id", out var id));
        Assert.AreEqual(JsonValueKind.String, id.ValueKind);
        Assert.AreNotEqual(Guid.Empty.ToString(), id.GetString());
    }

    [TestMethod]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "dupe@endpoint.test",
                password = Password,
                userName = "dupe1",
            }
        );
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var dupResponse = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "dupe@endpoint.test",
                password = Password,
                userName = "dupe2",
            }
        );

        Assert.AreEqual(HttpStatusCode.Conflict, dupResponse.StatusCode);
        var body = await ReadJsonAsync(dupResponse);
        Assert.AreEqual("Email is already registered.", body.GetProperty("message").GetString());
    }

    [TestMethod]
    public async Task Register_InvalidData_Returns400WithValidationErrors()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = "",
                password = "12",
                userName = "",
            }
        );

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await ReadJsonAsync(response);
        Assert.IsTrue(body.TryGetProperty("errors", out var errors));
        Assert.AreEqual(JsonValueKind.Object, errors.ValueKind);
        Assert.IsTrue(errors.TryGetProperty("UserName", out _));
        Assert.IsTrue(errors.TryGetProperty("Email", out _));
        Assert.IsTrue(errors.TryGetProperty("Password", out _));
    }

    [TestMethod]
    public async Task Login_WithRegisteredUser_Returns200WithAccessToken()
    {
        var email = $"login-ok-{Guid.NewGuid():N}@test.com";
        await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = Password,
                userName = "loginok",
            }
        );

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = Password }
        );

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.IsTrue(body.TryGetProperty("access_token", out var token));
        Assert.IsFalse(string.IsNullOrEmpty(token.GetString()));
    }

    [TestMethod]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"login-wrong-{Guid.NewGuid():N}@test.com";
        await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = Password,
                userName = "loginwrong",
            }
        );

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "wrongpassword" }
        );

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.AreEqual("Invalid email or password.", body.GetProperty("detail").GetString());
    }

    [TestMethod]
    public async Task RegisterThenLoginThenRefresh_ReturnsNewTokens()
    {
        var email = $"refresh-flow-{Guid.NewGuid():N}@test.com";
        await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = Password,
                userName = "refreshflow",
            }
        );

        var loginRes = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = Password }
        );
        var loginBody = await ReadJsonAsync(loginRes);

        // The refresh token rides in an httpOnly cookie the test client persists from login.
        var refreshRes = await Client.PostAsync("/api/auth/refresh", null);

        Assert.AreEqual(HttpStatusCode.OK, refreshRes.StatusCode);
        var refreshBody = await ReadJsonAsync(refreshRes);
        Assert.IsTrue(refreshBody.TryGetProperty("access_token", out var newToken));
        Assert.IsFalse(string.IsNullOrEmpty(newToken.GetString()));
        Assert.AreNotEqual(loginBody.GetProperty("access_token").GetString(), newToken.GetString());
    }

    [TestMethod]
    public async Task RegisterThenLoginThenLogout_Succeeds()
    {
        var email = $"logout-flow-{Guid.NewGuid():N}@test.com";
        await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = Password,
                userName = "logoutflow",
            }
        );

        var loginRes = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = Password }
        );
        var loginBody = await ReadJsonAsync(loginRes);
        var accessToken = loginBody.GetProperty("access_token").GetString();

        var logoutRes = await SendAsync(HttpMethod.Post, "/api/auth/logout", accessToken);

        Assert.AreEqual(HttpStatusCode.OK, logoutRes.StatusCode);
        var logoutBody = await ReadJsonAsync(logoutRes);
        Assert.IsTrue(logoutBody.GetProperty("success").GetBoolean());
    }
}
