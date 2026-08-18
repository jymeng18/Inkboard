#pragma warning disable MSTEST0049

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inkboard.Tests.Workflows;

/// <summary>
/// Complete user journeys exercised end-to-end over HTTP through the real API,
/// services, and in-memory database. Each test walks a full expected workflow
/// (register → collaborate → tear down), asserting the business outcome the user
/// would actually observe, not internal state.
/// </summary>
[TestClass]
public sealed class UserWorkflowTests
{
    private static IntegrationTestFactory _factory = null!;
    private static HttpClient _client = null!;

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

    // ─── HTTP helpers ───────────────────────────────────────────

    private static HttpRequestMessage Req(HttpMethod method, string url, string token, object? body = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            msg.Content = JsonContent.Create(body);
        return msg;
    }

    private static async Task<(Guid id, string token)> NewUserAsync(string name)
    {
        var email = $"{name}-{Guid.NewGuid():N}@wf.test";
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "TestPass123", userName = name });
        reg.EnsureSuccessStatusCode();
        var regBody = await reg.Content.ReadFromJsonAsync<JsonElement>();
        var id = Guid.Parse(regBody.GetProperty("id").GetString()!);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "TestPass123" });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("access_token").GetString()!;

        return (id, token);
    }

    private static async Task<Guid> CreateCanvasAsync(string token, string name = "Canvas")
    {
        var res = await _client.SendAsync(Req(HttpMethod.Post, "/api/canvas", token, new { name }));
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task<Guid> CreatePartyAsync(string token, Guid canvasId)
    {
        var res = await _client.SendAsync(Req(HttpMethod.Post, "/api/parties", token, new { canvasId }));
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task<Guid> InviteAsync(string leaderToken, Guid partyId, Guid invitedUserId)
    {
        var res = await _client.SendAsync(Req(HttpMethod.Post, $"/api/parties/{partyId}/invites", leaderToken,
            new { invitedUserId = invitedUserId.ToString() }));
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(body.GetProperty("id").GetString()!);
    }

    private static async Task RespondAsync(string memberToken, Guid inviteId, bool accepted)
    {
        var res = await _client.SendAsync(Req(HttpMethod.Post, $"/api/invites/{inviteId}/respond", memberToken, new { accepted }));
        res.EnsureSuccessStatusCode();
    }

    private static async Task JoinPartyAsync(string leaderToken, Guid partyId, Guid memberId, string memberToken)
    {
        var inviteId = await InviteAsync(leaderToken, partyId, memberId);
        await RespondAsync(memberToken, inviteId, true);
    }

    private static async Task<JsonElement> GetPartyAsync(string token, Guid partyId)
    {
        var res = await _client.SendAsync(Req(HttpMethod.Get, $"/api/parties/{partyId}", token));
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static List<(Guid userId, string role)> MembersOf(JsonElement party) =>
        party.GetProperty("members").EnumerateArray()
            .Select(m => (Guid.Parse(m.GetProperty("userId").GetString()!), m.GetProperty("role").GetString()!))
            .ToList();

    // ─── Canvas lifecycle ───────────────────────────────────────

    [TestMethod]
    public async Task Workflow_RegisterLoginCreateRenameCanvas_ReflectedInGallery()
    {
        var (_, token) = await NewUserAsync("alice");
        var canvasId = await CreateCanvasAsync(token, "My First Board");

        var renameRes = await _client.SendAsync(Req(HttpMethod.Put, $"/api/canvas/{canvasId}", token, new { name = "Renamed Board" }));
        Assert.AreEqual(HttpStatusCode.NoContent, renameRes.StatusCode);

        var listRes = await _client.SendAsync(Req(HttpMethod.Get, "/api/canvas", token));
        var list = await listRes.Content.ReadFromJsonAsync<JsonElement>();
        var match = list.EnumerateArray().Single(c => Guid.Parse(c.GetProperty("id").GetString()!) == canvasId);
        Assert.AreEqual("Renamed Board", match.GetProperty("name").GetString());
    }

    [TestMethod]
    public async Task Workflow_DeleteCanvas_DisappearsFromGallery()
    {
        var (_, token) = await NewUserAsync("bob");
        var canvasId = await CreateCanvasAsync(token);

        var delRes = await _client.SendAsync(Req(HttpMethod.Delete, $"/api/canvas/{canvasId}", token));
        Assert.AreEqual(HttpStatusCode.NoContent, delRes.StatusCode);

        var listRes = await _client.SendAsync(Req(HttpMethod.Get, "/api/canvas", token));
        var list = await listRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsFalse(list.EnumerateArray().Any(c => Guid.Parse(c.GetProperty("id").GetString()!) == canvasId));
    }

    // ─── Party formation ────────────────────────────────────────

    [TestMethod]
    public async Task Workflow_InviteAccept_BothAppearInPartyWithCorrectRoles()
    {
        var (leaderId, leaderToken) = await NewUserAsync("leader");
        var (memberId, memberToken) = await NewUserAsync("member");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);

        await JoinPartyAsync(leaderToken, partyId, memberId, memberToken);

        var members = MembersOf(await GetPartyAsync(leaderToken, partyId));
        Assert.HasCount(2, members);
        Assert.AreEqual("Leader", members.Single(m => m.userId == leaderId).role);
        Assert.AreEqual("Member", members.Single(m => m.userId == memberId).role);
    }

    [TestMethod]
    public async Task Workflow_InviteDeclined_InviteeNeverJoins()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (memberId, memberToken) = await NewUserAsync("decliner");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);

        var inviteId = await InviteAsync(leaderToken, partyId, memberId);
        await RespondAsync(memberToken, inviteId, accepted: false);

        var members = MembersOf(await GetPartyAsync(leaderToken, partyId));
        Assert.HasCount(1, members);
        Assert.IsFalse(members.Any(m => m.userId == memberId));
    }

    [TestMethod]
    public async Task Workflow_PartyFillsToFive_SixthInviteRejectedAsFull()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);

        // Four members join → party now holds the max of five with the leader.
        for (int i = 0; i < 4; i++)
        {
            var (mId, mToken) = await NewUserAsync($"filler{i}");
            await JoinPartyAsync(leaderToken, partyId, mId, mToken);
        }

        var (sixthId, _) = await NewUserAsync("sixth");
        var res = await _client.SendAsync(Req(HttpMethod.Post, $"/api/parties/{partyId}/invites", leaderToken,
            new { invitedUserId = sixthId.ToString() }));

        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.HasCount(5, MembersOf(await GetPartyAsync(leaderToken, partyId)));
    }

    // ─── Collaboration on the canvas ────────────────────────────

    [TestMethod]
    public async Task Workflow_MemberReadsOperationsOwnerPersisted()
    {
        var (_, leaderToken) = await NewUserAsync("owner");
        var (memberId, memberToken) = await NewUserAsync("collab");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);
        await JoinPartyAsync(leaderToken, partyId, memberId, memberToken);

        var saveRes = await _client.SendAsync(Req(HttpMethod.Post, $"/api/canvas/{canvasId}/operations", leaderToken,
            new { type = 0, operationData = "{\"stroke\":\"abc\"}" }));
        Assert.AreEqual(HttpStatusCode.NoContent, saveRes.StatusCode);

        var opsRes = await _client.SendAsync(Req(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", memberToken));
        Assert.AreEqual(HttpStatusCode.OK, opsRes.StatusCode);
        var ops = await opsRes.Content.ReadFromJsonAsync<List<string>>();
        CollectionAssert.Contains(ops, "{\"stroke\":\"abc\"}");
    }

    [TestMethod]
    public async Task Workflow_StrangerCannotReadCanvasOperations()
    {
        var (_, ownerToken) = await NewUserAsync("owner");
        var (_, strangerToken) = await NewUserAsync("stranger");
        var canvasId = await CreateCanvasAsync(ownerToken);
        await _client.SendAsync(Req(HttpMethod.Post, $"/api/canvas/{canvasId}/operations", ownerToken,
            new { type = 0, operationData = "{\"secret\":true}" }));

        var res = await _client.SendAsync(Req(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", strangerToken));

        Assert.AreEqual(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ─── Leadership + dissolution ───────────────────────────────

    [TestMethod]
    public async Task Workflow_LeaderLeavesThreePersonParty_OldestMemberBecomesLeader()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (firstId, firstToken) = await NewUserAsync("firstJoiner");
        var (secondId, secondToken) = await NewUserAsync("secondJoiner");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);
        // firstJoiner joins before secondJoiner, so it is the oldest member and
        // the one leadership must fall to when the leader steps out.
        await JoinPartyAsync(leaderToken, partyId, firstId, firstToken);
        await JoinPartyAsync(leaderToken, partyId, secondId, secondToken);

        var leaveRes = await _client.SendAsync(Req(HttpMethod.Delete, $"/api/parties/{partyId}", leaderToken));
        Assert.AreEqual(HttpStatusCode.NoContent, leaveRes.StatusCode);

        var party = await GetPartyAsync(firstToken, partyId);
        Assert.AreEqual(firstId, Guid.Parse(party.GetProperty("leaderId").GetString()!));
        Assert.AreEqual("Leader", MembersOf(party).Single(m => m.userId == firstId).role);
    }

    [TestMethod]
    public async Task Workflow_LeaderLeavesTwoPersonParty_PartyDissolves()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (memberId, memberToken) = await NewUserAsync("member");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);
        await JoinPartyAsync(leaderToken, partyId, memberId, memberToken);

        await _client.SendAsync(Req(HttpMethod.Delete, $"/api/parties/{partyId}", leaderToken));

        var res = await _client.SendAsync(Req(HttpMethod.Get, $"/api/parties/{partyId}", memberToken));
        Assert.AreEqual(HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task Workflow_LeaderKicksMember_MemberRemovedAndLosesCanvasAccess()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (memberId, memberToken) = await NewUserAsync("member");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);
        await JoinPartyAsync(leaderToken, partyId, memberId, memberToken);

        var kickRes = await _client.SendAsync(Req(HttpMethod.Delete, $"/api/parties/{partyId}/members/{memberId}", leaderToken));
        Assert.AreEqual(HttpStatusCode.NoContent, kickRes.StatusCode);

        Assert.IsFalse(MembersOf(await GetPartyAsync(leaderToken, partyId)).Any(m => m.userId == memberId));
        // Access to the canvas op-log rode on party membership, so it's gone too.
        var opsRes = await _client.SendAsync(Req(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", memberToken));
        Assert.AreEqual(HttpStatusCode.Forbidden, opsRes.StatusCode);
    }

    // ─── Blocking ───────────────────────────────────────────────

    [TestMethod]
    public async Task Workflow_LeaderBlocksUserThenInvites_InviteRejected()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (targetId, _) = await NewUserAsync("blocked");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);

        var blockRes = await _client.SendAsync(Req(HttpMethod.Post, $"/api/users/{targetId}/block", leaderToken));
        Assert.AreEqual(HttpStatusCode.NoContent, blockRes.StatusCode);

        var inviteRes = await _client.SendAsync(Req(HttpMethod.Post, $"/api/parties/{partyId}/invites", leaderToken,
            new { invitedUserId = targetId.ToString() }));
        Assert.AreEqual(HttpStatusCode.BadRequest, inviteRes.StatusCode);
    }

    [TestMethod]
    public async Task Workflow_LeaderBlocksInviteeAfterInvite_AcceptForbidden()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (targetId, targetToken) = await NewUserAsync("invitee");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);
        var inviteId = await InviteAsync(leaderToken, partyId, targetId);

        // Leader blocks the invitee after the invite went out but before they answer.
        await _client.SendAsync(Req(HttpMethod.Post, $"/api/users/{targetId}/block", leaderToken));

        var respondRes = await _client.SendAsync(Req(HttpMethod.Post, $"/api/invites/{inviteId}/respond", targetToken, new { accepted = true }));
        Assert.AreEqual(HttpStatusCode.Forbidden, respondRes.StatusCode);
        Assert.IsFalse(MembersOf(await GetPartyAsync(leaderToken, partyId)).Any(m => m.userId == targetId));
    }

    // ─── Moving the party between canvases + ending ─────────────

    [TestMethod]
    public async Task Workflow_LeaderMovesPartyToNewCanvas_MemberGainsAccess()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (memberId, memberToken) = await NewUserAsync("member");
        var firstCanvas = await CreateCanvasAsync(leaderToken, "First");
        var partyId = await CreatePartyAsync(leaderToken, firstCanvas);
        await JoinPartyAsync(leaderToken, partyId, memberId, memberToken);
        var secondCanvas = await CreateCanvasAsync(leaderToken, "Second");

        // Before the move, the member has no claim on the second canvas.
        var before = await _client.SendAsync(Req(HttpMethod.Get, $"/api/canvas/{secondCanvas}/operations", memberToken));
        Assert.AreEqual(HttpStatusCode.Forbidden, before.StatusCode);

        var patchRes = await _client.SendAsync(Req(HttpMethod.Patch, $"/api/parties/{partyId}/canvas", leaderToken,
            new { canvasId = secondCanvas.ToString() }));
        Assert.AreEqual(HttpStatusCode.NoContent, patchRes.StatusCode);

        var after = await _client.SendAsync(Req(HttpMethod.Get, $"/api/canvas/{secondCanvas}/operations", memberToken));
        Assert.AreEqual(HttpStatusCode.OK, after.StatusCode);
    }

    [TestMethod]
    public async Task Workflow_LeaderEndsSession_PartyGoneCanvasKept()
    {
        var (_, leaderToken) = await NewUserAsync("leader");
        var (memberId, memberToken) = await NewUserAsync("member");
        var canvasId = await CreateCanvasAsync(leaderToken);
        var partyId = await CreatePartyAsync(leaderToken, canvasId);
        await JoinPartyAsync(leaderToken, partyId, memberId, memberToken);

        var endRes = await _client.SendAsync(Req(HttpMethod.Post, $"/api/parties/{partyId}/end", leaderToken));
        Assert.AreEqual(HttpStatusCode.NoContent, endRes.StatusCode);

        var partyRes = await _client.SendAsync(Req(HttpMethod.Get, $"/api/parties/{partyId}", leaderToken));
        Assert.AreEqual(HttpStatusCode.NotFound, partyRes.StatusCode);

        var listRes = await _client.SendAsync(Req(HttpMethod.Get, "/api/canvas", leaderToken));
        var list = await listRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(list.EnumerateArray().Any(c => Guid.Parse(c.GetProperty("id").GetString()!) == canvasId));
    }

    // ─── Friends lifecycle ──────────────────────────────────────

    [TestMethod]
    public async Task Workflow_FriendRequestAccepted_BothListEachOther()
    {
        var (aId, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");

        var sendRes = await _client.SendAsync(Req(HttpMethod.Post, $"/api/friends/{bId}", aToken));
        Assert.AreEqual(HttpStatusCode.Created, sendRes.StatusCode);

        // B finds the incoming request and accepts it.
        var reqsRes = await _client.SendAsync(Req(HttpMethod.Get, "/api/friend-requests", bToken));
        var reqs = await reqsRes.Content.ReadFromJsonAsync<JsonElement>();
        var incoming = reqs.EnumerateArray().Single(r => Guid.Parse(r.GetProperty("userId").GetString()!) == aId);
        var requestId = Guid.Parse(incoming.GetProperty("id").GetString()!);

        var acceptRes = await _client.SendAsync(Req(HttpMethod.Patch, $"/api/friend-requests/{requestId}", bToken,
            new { requesterId = aId.ToString(), accepted = true }));
        Assert.AreEqual(HttpStatusCode.OK, acceptRes.StatusCode);

        Assert.IsTrue(await FriendListContainsAsync(aToken, bId));
        Assert.IsTrue(await FriendListContainsAsync(bToken, aId));
    }

    [TestMethod]
    public async Task Workflow_FriendRequestRejected_NoFriendship()
    {
        var (aId, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");
        await _client.SendAsync(Req(HttpMethod.Post, $"/api/friends/{bId}", aToken));

        var reqsRes = await _client.SendAsync(Req(HttpMethod.Get, "/api/friend-requests", bToken));
        var reqs = await reqsRes.Content.ReadFromJsonAsync<JsonElement>();
        var requestId = Guid.Parse(reqs.EnumerateArray().Single(r => Guid.Parse(r.GetProperty("userId").GetString()!) == aId).GetProperty("id").GetString()!);

        var rejectRes = await _client.SendAsync(Req(HttpMethod.Patch, $"/api/friend-requests/{requestId}", bToken,
            new { requesterId = aId.ToString(), accepted = false }));
        Assert.AreEqual(HttpStatusCode.OK, rejectRes.StatusCode);

        Assert.IsFalse(await FriendListContainsAsync(aToken, bId));
        Assert.IsFalse(await FriendListContainsAsync(bToken, aId));
    }

    [TestMethod]
    public async Task Workflow_SenderCancelsRequestThenResends_Succeeds()
    {
        var (_, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");

        var send1 = await _client.SendAsync(Req(HttpMethod.Post, $"/api/friends/{bId}", aToken));
        var send1Body = await send1.Content.ReadFromJsonAsync<JsonElement>();
        var requestId = Guid.Parse(send1Body.GetProperty("id").GetString()!);

        var cancelRes = await _client.SendAsync(Req(HttpMethod.Delete, $"/api/friend-requests/{requestId}", aToken));
        Assert.AreEqual(HttpStatusCode.NoContent, cancelRes.StatusCode);

        // B's inbox no longer shows the cancelled request as pending (status enum
        // serializes as its numeric value; Pending == 0).
        var reqsRes = await _client.SendAsync(Req(HttpMethod.Get, "/api/friend-requests", bToken));
        var reqs = await reqsRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsFalse(reqs.EnumerateArray().Any(r =>
            Guid.Parse(r.GetProperty("id").GetString()!) == requestId
            && r.GetProperty("status").GetInt32() == 0));

        // And a brand-new request is allowed after the cancel.
        var send2 = await _client.SendAsync(Req(HttpMethod.Post, $"/api/friends/{bId}", aToken));
        Assert.AreEqual(HttpStatusCode.Created, send2.StatusCode);
    }

    [TestMethod]
    public async Task Workflow_UnfriendThenReRequest_Succeeds()
    {
        var (aId, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");
        await _client.SendAsync(Req(HttpMethod.Post, $"/api/friends/{bId}", aToken));
        var reqsRes = await _client.SendAsync(Req(HttpMethod.Get, "/api/friend-requests", bToken));
        var reqs = await reqsRes.Content.ReadFromJsonAsync<JsonElement>();
        var requestId = Guid.Parse(reqs.EnumerateArray().Single(r => Guid.Parse(r.GetProperty("userId").GetString()!) == aId).GetProperty("id").GetString()!);
        await _client.SendAsync(Req(HttpMethod.Patch, $"/api/friend-requests/{requestId}", bToken, new { requesterId = aId.ToString(), accepted = true }));

        var unfriendRes = await _client.SendAsync(Req(HttpMethod.Delete, $"/api/friends/{bId}", aToken));
        Assert.AreEqual(HttpStatusCode.NoContent, unfriendRes.StatusCode);
        Assert.IsFalse(await FriendListContainsAsync(aToken, bId));
        Assert.IsFalse(await FriendListContainsAsync(bToken, aId));

        // Nothing stops a fresh request after unfriending.
        var reSend = await _client.SendAsync(Req(HttpMethod.Post, $"/api/friends/{bId}", aToken));
        Assert.AreEqual(HttpStatusCode.Created, reSend.StatusCode);
    }

    // ─── Auth gate ──────────────────────────────────────────────

    [TestMethod]
    public async Task Workflow_ProtectedEndpointWithoutToken_Unauthorized()
    {
        var res = await _client.GetAsync("/api/canvas");
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ─── local helpers ──────────────────────────────────────────

    private static async Task<bool> FriendListContainsAsync(string token, Guid friendId)
    {
        var res = await _client.SendAsync(Req(HttpMethod.Get, "/api/friends", token));
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<JsonElement>();
        return list.EnumerateArray().Any(f => Guid.Parse(f.GetProperty("userId").GetString()!) == friendId);
    }
}
