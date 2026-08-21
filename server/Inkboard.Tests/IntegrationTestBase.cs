using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inkboard.Tests;

/// <summary>
/// Base for HTTP-level integration tests. Boots a fresh in-memory app and client
/// per test — so every test gets its own database with no shared state — and
/// carries the request/JSON plumbing those tests repeat.
/// </summary>
public abstract class IntegrationTestBase
{
    private IntegrationTestFactory _factory = null!;
    protected HttpClient Client { get; private set; } = null!;

    [TestInitialize]
    public void InitializeClient()
    {
        _factory = new IntegrationTestFactory();
        Client = _factory.CreateClient();
    }

    [TestCleanup]
    public void DisposeClient()
    {
        Client?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>Builds a request carrying an optional bearer token and JSON body.</summary>
    protected static HttpRequestMessage Req(
        HttpMethod method,
        string url,
        string? token = null,
        object? body = null
    )
    {
        var msg = new HttpRequestMessage(method, url);
        if (token is not null)
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            msg.Content = JsonContent.Create(body);
        return msg;
    }

    /// <summary>Sends a request built by <see cref="Req"/> and returns the raw response.</summary>
    protected Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string? token = null,
        object? body = null
    ) => Client.SendAsync(Req(method, url, token, body));

    protected static Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<JsonElement>();

    protected static Guid IdOf(JsonElement body) => Guid.Parse(body.GetProperty("id").GetString()!);

    protected async Task<(Guid Id, string Token)> NewUserAsync(string name)
    {
        var email = $"{name}-{Guid.NewGuid():N}@it.test";
        var reg = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = "TestPass123",
                userName = name,
            }
        );
        reg.EnsureSuccessStatusCode();
        var id = IdOf(await ReadJsonAsync(reg));

        var login = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "TestPass123" }
        );
        login.EnsureSuccessStatusCode();
        var token = (await ReadJsonAsync(login)).GetProperty("access_token").GetString()!;

        return (id, token);
    }
}
