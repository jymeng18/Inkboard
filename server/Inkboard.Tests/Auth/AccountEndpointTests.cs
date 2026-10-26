using System.Net;
using System.Net.Http.Json;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class AccountEndpointTests : IntegrationTestBase
{
    [TestMethod]
    public async Task GetAccountAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/users/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var response = await SendAsync(HttpMethod.Delete, "/api/users/me", body: new { password = "TestPass123" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetAccountAsync_WhenAuthenticated_ReturnsOwnProfileWithoutPassword()
    {
        var (userId, token) = await NewUserAsync("artist");

        var response = await SendAsync(HttpMethod.Get, "/api/users/me", token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var account = await ReadJsonAsync(response);
        Assert.AreEqual(userId, account.GetProperty("id").GetGuid());
        Assert.AreEqual("artist", account.GetProperty("userName").GetString());
        Assert.IsTrue(account.GetProperty("email").GetString()!.EndsWith("@it.test"));
        Assert.IsTrue(account.TryGetProperty("createdAt", out _));
        Assert.IsFalse(account.TryGetProperty("passwordHash", out _));
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenPasswordIsWrong_AccountStillAccessible()
    {
        var (_, token) = await NewUserAsync("artist");

        var response = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password = "wrong-password" });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        var account = await SendAsync(HttpMethod.Get, "/api/users/me", token);
        Assert.AreEqual(HttpStatusCode.OK, account.StatusCode);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    public async Task DeleteAccountAsync_WhenPasswordIsMissing_ReturnsBadRequest(string? password)
    {
        var (_, token) = await NewUserAsync("artist");

        var response = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenConfirmed_RejectsOldAccessToken()
    {
        var (_, token) = await NewUserAsync("artist");

        var deletion = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password = "TestPass123" });

        Assert.AreEqual(HttpStatusCode.NoContent, deletion.StatusCode);
        var response = await SendAsync(HttpMethod.Get, "/api/canvas", token);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenConfirmed_RefreshCannotRestoreSession()
    {
        var (_, token) = await NewUserAsync("artist");

        var deletion = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password = "TestPass123" });
        deletion.EnsureSuccessStatusCode();

        var response = await Client.PostAsync("/api/auth/refresh", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenConfirmed_ClearsRefreshCookieOnAuthPath()
    {
        var (_, token) = await NewUserAsync("artist");

        var response = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password = "TestPass123" });

        response.EnsureSuccessStatusCode();
        Assert.IsTrue(response.Headers.GetValues("Set-Cookie")
            .Any(cookie => cookie.StartsWith("refreshToken=;") && cookie.Contains("path=/api/auth")));
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenConfirmed_RemovesOwnedCanvas()
    {
        var (_, token) = await NewUserAsync("artist");
        var canvas = await SendAsync(HttpMethod.Post, "/api/canvas", token, new { name = "My canvas" });
        canvas.EnsureSuccessStatusCode();
        var canvasId = IdOf(await ReadJsonAsync(canvas));

        var deletion = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password = "TestPass123" });
        deletion.EnsureSuccessStatusCode();

        var (_, otherToken) = await NewUserAsync("other");
        var response = await SendAsync(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", otherToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenLastPartyLeader_ClosesParty()
    {
        var (_, token) = await NewUserAsync("leader");
        var partyId = await CreatePartyAsync(token);

        var deletion = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password = "TestPass123" });
        deletion.EnsureSuccessStatusCode();

        var (_, otherToken) = await NewUserAsync("other");
        var response = await SendAsync(HttpMethod.Get, $"/api/parties/{partyId}", otherToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenLeaderHasTwoOtherMembers_TransfersLeadership()
    {
        var (_, token) = await NewUserAsync("leader");
        var (firstMemberId, firstToken) = await NewUserAsync("first");
        var (secondMemberId, secondToken) = await NewUserAsync("second");
        var partyId = await CreatePartyAsync(token);
        await JoinPartyAsync(token, partyId, firstMemberId, firstToken);
        await JoinPartyAsync(token, partyId, secondMemberId, secondToken);

        var deletion = await SendAsync(HttpMethod.Delete, "/api/users/me", token, new { password = "TestPass123" });
        deletion.EnsureSuccessStatusCode();

        var response = await SendAsync(HttpMethod.Get, $"/api/parties/{partyId}", firstToken);
        response.EnsureSuccessStatusCode();
        var party = await ReadJsonAsync(response);
        Assert.AreEqual(firstMemberId, party.GetProperty("leaderId").GetGuid());
        Assert.AreEqual(2, party.GetProperty("members").GetArrayLength());
    }

    [TestMethod]
    public async Task DeleteAccountAsync_WhenContributor_LeavesDrawingOnOtherUsersCanvas()
    {
        var (_, ownerToken) = await NewUserAsync("owner");
        var (memberId, memberToken) = await NewUserAsync("member");
        var partyId = await CreatePartyAsync(ownerToken);
        var partyResponse = await SendAsync(HttpMethod.Get, $"/api/parties/{partyId}", ownerToken);
        var canvasId = (await ReadJsonAsync(partyResponse)).GetProperty("canvasId").GetGuid();
        await JoinPartyAsync(ownerToken, partyId, memberId, memberToken);
        var operation = await SendAsync(HttpMethod.Post, $"/api/canvas/{canvasId}/operations", memberToken,
            new { type = 0, operationData = "a contribution worth keeping" });
        operation.EnsureSuccessStatusCode();

        var deletion = await SendAsync(HttpMethod.Delete, "/api/users/me", memberToken, new { password = "TestPass123" });
        deletion.EnsureSuccessStatusCode();

        var response = await SendAsync(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", ownerToken);
        response.EnsureSuccessStatusCode();
        Assert.AreEqual("a contribution worth keeping", (await ReadJsonAsync(response))[0].GetString());
    }

    private async Task<Guid> CreatePartyAsync(string token)
    {
        var canvas = await SendAsync(HttpMethod.Post, "/api/canvas", token, new { name = "Party canvas" });
        canvas.EnsureSuccessStatusCode();
        var canvasId = IdOf(await ReadJsonAsync(canvas));
        var response = await SendAsync(HttpMethod.Post, "/api/parties", token, new { canvasId });
        response.EnsureSuccessStatusCode();
        return IdOf(await ReadJsonAsync(response));
    }

    private async Task JoinPartyAsync(string leaderToken, Guid partyId, Guid userId, string memberToken)
    {
        var invitation = await SendAsync(HttpMethod.Post, $"/api/parties/{partyId}/invites", leaderToken,
            new { invitedUserId = userId.ToString() });
        invitation.EnsureSuccessStatusCode();
        var inviteId = IdOf(await ReadJsonAsync(invitation));
        var response = await SendAsync(HttpMethod.Post, $"/api/invites/{inviteId}/respond", memberToken, new { accepted = true });
        response.EnsureSuccessStatusCode();
    }
}
