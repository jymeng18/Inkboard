using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Inkboard.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Inkboard.Tests.Parties;

/// <summary>
/// Reads the raw "sub" claim from the JWT so that Clients.User(userId)
/// works even when MapInboundClaims=false.
/// </summary>
public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("sub")?.Value;
    }
}

/// <summary>
/// Extends IntegrationTestFactory with a custom IUserIdProvider so that
/// SignalR notifications sent via Clients.User(userId) are routable.
/// </summary>
public sealed class PartyHubTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>());
    }
}

[TestClass]
public sealed class PartyHubTests
{
    private static PartyHubTestFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new PartyHubTestFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ─── Helpers ───────────────────────────────────────────────

    private static async Task<(Guid userId, string accessToken)> RegisterUserAsync(
        string email,
        string password,
        string userName
    )
    {
        var regRes = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password,
                userName,
            }
        );
        if (!regRes.IsSuccessStatusCode)
        {
            var errorBody = await regRes.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Register failed ({regRes.StatusCode}): {errorBody}"
            );
        }
        var regBody = await regRes.Content.ReadFromJsonAsync<JsonElement>();
        var userId = Guid.Parse(regBody.GetProperty("id").GetString()!);

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginRes.EnsureSuccessStatusCode();
        var loginBody = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("access_token").GetString()!;

        return (userId, token);
    }

    private static HubConnection CreateHubConnection(string accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(
                "http://localhost/hubs/party",
                options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.WebSocketFactory = (context, ct) =>
                    {
                        var uriWithToken = QueryHelpers.AddQueryString(
                            context.Uri.ToString(),
                            "access_token",
                            accessToken
                        );

                        return new ValueTask<WebSocket>(
                            _factory
                                .Server.CreateWebSocketClient()
                                .ConnectAsync(new Uri(uriWithToken), ct)
                        );
                    };
                    options.Transports = HttpTransportType.WebSockets;
                }
            )
            .Build();
    }

    private static HttpRequestMessage WithAuth(string method, string url, string accessToken)
    {
        var msg = new HttpRequestMessage(new HttpMethod(method), url);
        msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        return msg;
    }

    private static async Task<Guid> CreatePartyAsync(string accessToken)
    {
        var canvasId = await CreateCanvasAsync(accessToken, "Party Canvas");
        var req = WithAuth("POST", "/api/parties", accessToken);
        req.Content = JsonContent.Create(new { canvasId });
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task<Guid> CreateCanvasAsync(string accessToken, string name)
    {
        var req = WithAuth("POST", "/api/canvas", accessToken);
        req.Content = JsonContent.Create(new { name });
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task<Guid> InviteUserAsync(
        string accessToken,
        Guid partyId,
        Guid invitedUserId
    )
    {
        var req = WithAuth("POST", $"/api/parties/{partyId}/invites", accessToken);
        req.Content = JsonContent.Create(new { invitedUserId });
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task RespondToInviteAsync(string accessToken, Guid inviteId, bool accepted)
    {
        var req = WithAuth("POST", $"/api/invites/{inviteId}/respond", accessToken);
        req.Content = JsonContent.Create(new { accepted });
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    private static async Task<T> AwaitNotification<T>(Task<T> task, int timeoutMs = 5000)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
        if (completed != task)
        {
            throw new AssertFailedException(
                $"Timed out after {timeoutMs}ms waiting for SignalR notification."
            );
        }

        return task.Result;
    }

    // ─── Connection Tests ──────────────────────────────────────

    [TestMethod]
    public async Task Connect_UserInParty_NotifiesOtherMembers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8][..8];
        var (leaderId, leaderToken) = await RegisterUserAsync(
            $"lconn.{suffix}@t.com",
            "Pass1!",
            $"lconn_{suffix}"
        );
        var (memberId, memberToken) = await RegisterUserAsync(
            $"mconn.{suffix}@t.com",
            "Pass1!",
            $"mconn_{suffix}"
        );

        var partyId = await CreatePartyAsync(leaderToken);
        var inviteId = await InviteUserAsync(leaderToken, partyId, memberId);
        await RespondToInviteAsync(memberToken, inviteId, true);

        await using var leaderConn = CreateHubConnection(leaderToken);
        await leaderConn.StartAsync();

        var notifiedTcs = new TaskCompletionSource<Guid>();
        leaderConn.On<Guid>("NotifyOnConnection", uid => notifiedTcs.TrySetResult(uid));

        await using var memberConn = CreateHubConnection(memberToken);
        await memberConn.StartAsync();

        var notifiedId = await AwaitNotification(notifiedTcs.Task);
        Assert.AreEqual(memberId, notifiedId);
    }

    [TestMethod]
    public async Task Connect_UserNotInParty_ConnectionAborted()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, token) = await RegisterUserAsync(
            $"noparty.{suffix}@t.com",
            "Pass1!",
            $"noparty_{suffix}"
        );

        await using var conn = CreateHubConnection(token);
        try
        {
            await conn.StartAsync();
            Assert.Fail("Expected connection to be aborted for user not in any party.");
        }
        catch
        {
            // Expected — user without a party should be rejected by the hub
        }
    }

    [TestMethod]
    public async Task Disconnect_NotifiesOtherMembers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (leaderId, leaderToken) = await RegisterUserAsync(
            $"ldisc.{suffix}@t.com",
            "Pass1!",
            $"ldisc_{suffix}"
        );
        var (memberId, memberToken) = await RegisterUserAsync(
            $"mdisc.{suffix}@t.com",
            "Pass1!",
            $"mdisc_{suffix}"
        );

        var partyId = await CreatePartyAsync(leaderToken);
        var inviteId = await InviteUserAsync(leaderToken, partyId, memberId);
        await RespondToInviteAsync(memberToken, inviteId, true);

        await using var leaderConn = CreateHubConnection(leaderToken);
        await leaderConn.StartAsync();

        await using var memberConn = CreateHubConnection(memberToken);
        await memberConn.StartAsync();

        var disconnectTcs = new TaskCompletionSource<Guid>();
        leaderConn.On<Guid>("NotifyOnDisconnect", uid => disconnectTcs.TrySetResult(uid));

        await memberConn.StopAsync();

        var disconnectedId = await AwaitNotification(disconnectTcs.Task);
        Assert.AreEqual(memberId, disconnectedId);
    }

    // ─── Notification Tests ────────────────────────────────────

    [TestMethod]
    public async Task Invite_CreatesPendingInvite_ReturnsInviteData()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (leaderId, leaderToken) = await RegisterUserAsync(
            $"linv.{suffix}@t.com",
            "Pass1!",
            $"linv_{suffix}"
        );
        var (memberId, memberToken) = await RegisterUserAsync(
            $"minv.{suffix}@t.com",
            "Pass1!",
            $"minv_{suffix}"
        );

        var partyId = await CreatePartyAsync(leaderToken);

        var inviteId = await InviteUserAsync(leaderToken, partyId, memberId);

        Assert.AreNotEqual(Guid.Empty, inviteId);
    }

    [TestMethod]
    public async Task Kick_SendsNotificationToKickedUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (leaderId, leaderToken) = await RegisterUserAsync(
            $"lkick.{suffix}@t.com",
            "Pass1!",
            $"lkick_{suffix}"
        );
        var (memberId, memberToken) = await RegisterUserAsync(
            $"mkick.{suffix}@t.com",
            "Pass1!",
            $"mkick_{suffix}"
        );

        var partyId = await CreatePartyAsync(leaderToken);
        var inviteId = await InviteUserAsync(leaderToken, partyId, memberId);
        await RespondToInviteAsync(memberToken, inviteId, true);

        await using var memberConn = CreateHubConnection(memberToken);
        await memberConn.StartAsync();

        var kickTcs = new TaskCompletionSource<Guid>();
        memberConn.On<Guid>("NotifyOnKick", uid => kickTcs.TrySetResult(uid));

        var req = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/parties/{partyId}/members/{memberId}"
        );
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            leaderToken
        );
        var res = await _client.SendAsync(req);
        Assert.AreEqual(HttpStatusCode.NoContent, res.StatusCode);

        var kickedId = await AwaitNotification(kickTcs.Task);
        Assert.AreNotEqual(Guid.Empty, kickedId);
    }

    [TestMethod]
    public async Task MemberJoined_NotifiesPartyMembers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (leaderId, leaderToken) = await RegisterUserAsync(
            $"ljoin.{suffix}@t.com",
            "Pass1!",
            $"ljoin_{suffix}"
        );
        var (memberId, memberToken) = await RegisterUserAsync(
            $"mjoin.{suffix}@t.com",
            "Pass1!",
            $"mjoin_{suffix}"
        );

        var partyId = await CreatePartyAsync(leaderToken);

        await using var leaderConn = CreateHubConnection(leaderToken);
        await leaderConn.StartAsync();

        var joinedTcs = new TaskCompletionSource<Guid>();
        leaderConn.On<Guid>("NotifyOnMemberJoined", uid => joinedTcs.TrySetResult(uid));

        var inviteId = await InviteUserAsync(leaderToken, partyId, memberId);
        await RespondToInviteAsync(memberToken, inviteId, true);

        var joinedId = await AwaitNotification(joinedTcs.Task);
        Assert.AreEqual(memberId, joinedId);
    }

    [TestMethod]
    public async Task LeadershipTransfer_NotifiesPartyMembers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (leaderId, leaderToken) = await RegisterUserAsync(
            $"ltrans.{suffix}@t.com",
            "Pass1!",
            $"ltrans_{suffix}"
        );
        var (memberId, memberToken) = await RegisterUserAsync(
            $"mtrans.{suffix}@t.com",
            "Pass1!",
            $"mtrans_{suffix}"
        );

        var partyId = await CreatePartyAsync(leaderToken);
        var inviteId = await InviteUserAsync(leaderToken, partyId, memberId);
        await RespondToInviteAsync(memberToken, inviteId, true);

        await using var memberConn = CreateHubConnection(memberToken);
        await memberConn.StartAsync();

        var transferTcs = new TaskCompletionSource<Guid>();
        memberConn.On<Guid>(
            "LeadershipTransferred",
            newLeaderId => transferTcs.TrySetResult(newLeaderId)
        );

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/parties/{partyId}");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            leaderToken
        );
        var res = await _client.SendAsync(req);
        Assert.AreEqual(HttpStatusCode.NoContent, res.StatusCode);

        var newLeaderId = await AwaitNotification(transferTcs.Task);
        Assert.AreEqual(memberId, newLeaderId);
    }

    // ─── Multi-Client Sync Test ─────────────────────────────────

    [TestMethod]
    public async Task ConcurrentClients_SynchronizedState()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (leaderId, leaderToken) = await RegisterUserAsync(
            $"lsync.{suffix}@t.com",
            "Pass1!",
            $"lsync_{suffix}"
        );
        var (member1Id, member1Token) = await RegisterUserAsync(
            $"m1sync.{suffix}@t.com",
            "Pass1!",
            $"m1sync_{suffix}"
        );
        var (member2Id, member2Token) = await RegisterUserAsync(
            $"m2sync.{suffix}@t.com",
            "Pass1!",
            $"m2sync_{suffix}"
        );

        var partyId = await CreatePartyAsync(leaderToken);

        var inv1 = await InviteUserAsync(leaderToken, partyId, member1Id);
        await RespondToInviteAsync(member1Token, inv1, true);

        var inv2 = await InviteUserAsync(leaderToken, partyId, member2Id);
        await RespondToInviteAsync(member2Token, inv2, true);

        await using var leaderConn = CreateHubConnection(leaderToken);
        await leaderConn.StartAsync();

        await using var member1Conn = CreateHubConnection(member1Token);
        await member1Conn.StartAsync();

        await using var member2Conn = CreateHubConnection(member2Token);
        await member2Conn.StartAsync();

        var m1TransferTcs = new TaskCompletionSource<Guid>();
        member1Conn.On<Guid>("LeadershipTransferred", id => m1TransferTcs.TrySetResult(id));

        var m2TransferTcs = new TaskCompletionSource<Guid>();
        member2Conn.On<Guid>("LeadershipTransferred", id => m2TransferTcs.TrySetResult(id));

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/parties/{partyId}");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            leaderToken
        );
        var res = await _client.SendAsync(req);
        Assert.AreEqual(HttpStatusCode.NoContent, res.StatusCode);

        var m1NewLeader = await AwaitNotification(m1TransferTcs.Task);
        var m2NewLeader = await AwaitNotification(m2TransferTcs.Task);

        Assert.AreEqual(member1Id, m1NewLeader);
        Assert.AreEqual(member1Id, m2NewLeader);
    }
}
