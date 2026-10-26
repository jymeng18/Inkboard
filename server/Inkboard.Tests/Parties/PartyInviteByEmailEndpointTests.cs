using System.Net;
using System.Net.Http.Json;

namespace Inkboard.Tests.Parties;

[TestClass]
public sealed class PartyInviteByEmailEndpointTests : IntegrationTestBase
{
    [TestMethod]
    public async Task InviteByEmailAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var partyId = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync(
            $"/api/parties/{partyId}/invites/by-email",
            new { email = "friend@example.com" }
        );

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task InviteByEmailAsync_WhenEmailMatchesAccount_CreatesInviteForThatAccount()
    {
        var (_, token) = await NewUserAsync("leader");
        var partyId = await CreatePartyAsync(token);
        var email = $"friend-{Guid.NewGuid():N}@example.com";
        var registration = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password = "TestPass123", userName = "friend" }
        );
        registration.EnsureSuccessStatusCode();
        var invitedUserId = IdOf(await ReadJsonAsync(registration));

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/parties/{partyId}/invites/by-email",
            token,
            new { email }
        );

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var invite = await ReadJsonAsync(response);
        Assert.AreEqual(invitedUserId, invite.GetProperty("invitedUserId").GetGuid());
        Assert.AreEqual(partyId, invite.GetProperty("partyId").GetGuid());
    }

    [TestMethod]
    public async Task InviteByEmailAsync_WhenEmailIsUnknown_ReturnsNotFound()
    {
        var (_, token) = await NewUserAsync("leader");
        var partyId = await CreatePartyAsync(token);

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/parties/{partyId}/invites/by-email",
            token,
            new { email = "unknown@example.com" }
        );

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("No user found with this email address.", (await ReadJsonAsync(response)).GetString());
    }

    [TestMethod]
    public async Task InviteByEmailAsync_WhenEmailIsInvalid_ReturnsBadRequest()
    {
        var (_, token) = await NewUserAsync("leader");
        var partyId = await CreatePartyAsync(token);

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/parties/{partyId}/invites/by-email",
            token,
            new { email = "not-an-email" }
        );

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreatePartyAsync(string token)
    {
        var canvas = await SendAsync(HttpMethod.Post, "/api/canvas", token, new { name = "Email invites" });
        canvas.EnsureSuccessStatusCode();
        var canvasId = IdOf(await ReadJsonAsync(canvas));
        var response = await SendAsync(HttpMethod.Post, "/api/parties", token, new { canvasId });
        response.EnsureSuccessStatusCode();
        return IdOf(await ReadJsonAsync(response));
    }
}
