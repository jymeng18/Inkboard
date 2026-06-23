#pragma warning disable MSTEST0049

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inkboard.API.Tests.Auth;

[TestClass]
public sealed class AuthEndpointTests
{
    private static IntegrationTestFactory _factory = null!;
    private static HttpClient _client = null!;

    private const string Email = "endpoint@test.com";
    private const string Password = "TestPass123";
    private const string UserName = "endpointuser";

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new IntegrationTestFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Register_CreatesUser_Returns201WithId()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = Email,
            password = Password,
            userName = UserName,
        });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(body.TryGetProperty("id", out var id));
        Assert.AreEqual(JsonValueKind.String, id.ValueKind);
        Assert.AreNotEqual(Guid.Empty.ToString(), id.GetString());
    }

    [TestMethod]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "dupe@endpoint.test",
            password = Password,
            userName = "dupe1",
        });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var dupResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "dupe@endpoint.test",
            password = Password,
            userName = "dupe2",
        });

        Assert.AreEqual(HttpStatusCode.Conflict, dupResponse.StatusCode);
        var body = await dupResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("Email is already registered.", body.GetProperty("message").GetString());
    }

    [TestMethod]
    public async Task Register_InvalidData_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "",
            password = "12",
            userName = "",
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
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
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = Password,
            userName = "loginok",
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password,
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(body.TryGetProperty("access_token", out var token));
        Assert.IsFalse(string.IsNullOrEmpty(token.GetString()));
    }

    [TestMethod]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"login-wrong-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = Password,
            userName = "loginwrong",
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "wrongpassword",
        });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("Invalid email or password.", body.GetProperty("detail").GetString());
    }

    [TestMethod]
    public async Task RegisterThenLoginThenRefresh_ReturnsNewTokens()
    {
        var email = $"refresh-flow-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = Password,
            userName = "refreshflow",
        });

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password,
        });
        var loginBody = await loginRes.Content.ReadFromJsonAsync<JsonElement>();

        var refreshRes = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = loginBody.GetProperty("refresh_token").GetString(),
        });

        Assert.AreEqual(HttpStatusCode.OK, refreshRes.StatusCode);
        var refreshBody = await refreshRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(refreshBody.TryGetProperty("access_token", out var newToken));
        Assert.IsTrue(refreshBody.TryGetProperty("refresh_token", out var newRefresh));
        Assert.IsFalse(string.IsNullOrEmpty(newToken.GetString()));
        Assert.IsFalse(string.IsNullOrEmpty(newRefresh.GetString()));
        Assert.AreNotEqual(
            loginBody.GetProperty("access_token").GetString(),
            newToken.GetString());
    }

    [TestMethod]
    public async Task RegisterThenLoginThenLogout_Succeeds()
    {
        var email = $"logout-flow-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = Password,
            userName = "logoutflow",
        });

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password,
        });
        var loginBody = await loginRes.Content.ReadFromJsonAsync<JsonElement>();

        var logoutRes = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = loginBody.GetProperty("refresh_token").GetString(),
        });

        Assert.AreEqual(HttpStatusCode.OK, logoutRes.StatusCode);
        var logoutBody = await logoutRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(logoutBody.GetProperty("success").GetBoolean());
    }
}
