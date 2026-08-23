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
public sealed class UserWorkflowTests : IntegrationTestBase
{
    // ─── workflow API helpers ───────────────────────────────────

    private async Task<Guid> CreateCanvasAsync(string token, string name = "Canvas")
    {
        var res = await SendAsync(HttpMethod.Post, "/api/canvas", token, new { name });
        res.EnsureSuccessStatusCode();
        return IdOf(await ReadJsonAsync(res));
    }

    private async Task<Guid> CreatePartyAsync(string token, Guid canvasId)
    {
        var res = await SendAsync(HttpMethod.Post, "/api/parties", token, new { canvasId });
        res.EnsureSuccessStatusCode();
        return IdOf(await ReadJsonAsync(res));
    }

    private async Task<Guid> InviteAsync(string leaderToken, Guid partyId, Guid invitedUserId)
    {
        var res = await SendAsync(HttpMethod.Post, $"/api/parties/{partyId}/invites", leaderToken,
            new { invitedUserId = invitedUserId.ToString() });
        res.EnsureSuccessStatusCode();
        return IdOf(await ReadJsonAsync(res));
    }

    private async Task RespondAsync(string memberToken, Guid inviteId, bool accepted)
    {
        var res = await SendAsync(HttpMethod.Post, $"/api/invites/{inviteId}/respond", memberToken, new { accepted });
        res.EnsureSuccessStatusCode();
    }

    private async Task JoinPartyAsync(string leaderToken, Guid partyId, Guid memberId, string memberToken)
    {
        var inviteId = await InviteAsync(leaderToken, partyId, memberId);
        await RespondAsync(memberToken, inviteId, true);
    }

    private async Task<JsonElement> GetPartyAsync(string token, Guid partyId)
    {
        var res = await SendAsync(HttpMethod.Get, $"/api/parties/{partyId}", token);
        res.EnsureSuccessStatusCode();
        return await ReadJsonAsync(res);
    }

    private static List<(Guid userId, string role)> MembersOf(JsonElement party) =>
        party.GetProperty("members").EnumerateArray()
            .Select(m => (Guid.Parse(m.GetProperty("userId").GetString()!), m.GetProperty("role").GetString()!))
            .ToList();

    private async Task<bool> FriendListContainsAsync(string token, Guid friendId)
    {
        var res = await SendAsync(HttpMethod.Get, "/api/friends", token);
        res.EnsureSuccessStatusCode();
        var list = await ReadJsonAsync(res);
        return list.EnumerateArray().Any(f => Guid.Parse(f.GetProperty("userId").GetString()!) == friendId);
    }

    // ─── Canvas lifecycle ───────────────────────────────────────

    [TestMethod]
    public async Task Workflow_RegisterLoginCreateRenameCanvas_ReflectedInGallery()
    {
        var (_, token) = await NewUserAsync("alice");
        var canvasId = await CreateCanvasAsync(token, "My First Board");

        var renameRes = await SendAsync(HttpMethod.Put, $"/api/canvas/{canvasId}", token, new { name = "Renamed Board" });
        Assert.AreEqual(HttpStatusCode.NoContent, renameRes.StatusCode);

        var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/canvas", token));
        var match = list.EnumerateArray().Single(c => IdOf(c) == canvasId);
        Assert.AreEqual("Renamed Board", match.GetProperty("name").GetString());
    }

    [TestMethod]
    public async Task Workflow_DeleteCanvas_DisappearsFromGallery()
    {
        var (_, token) = await NewUserAsync("bob");
        var canvasId = await CreateCanvasAsync(token);

        var delRes = await SendAsync(HttpMethod.Delete, $"/api/canvas/{canvasId}", token);
        Assert.AreEqual(HttpStatusCode.NoContent, delRes.StatusCode);

        var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/canvas", token));
        Assert.IsFalse(list.EnumerateArray().Any(c => IdOf(c) == canvasId));
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
        var res = await SendAsync(HttpMethod.Post, $"/api/parties/{partyId}/invites", leaderToken,
            new { invitedUserId = sixthId.ToString() });

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

        var saveRes = await SendAsync(HttpMethod.Post, $"/api/canvas/{canvasId}/operations", leaderToken,
            new { type = 0, operationData = "{\"stroke\":\"abc\"}" });
        Assert.AreEqual(HttpStatusCode.NoContent, saveRes.StatusCode);

        var opsRes = await SendAsync(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", memberToken);
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
        await SendAsync(HttpMethod.Post, $"/api/canvas/{canvasId}/operations", ownerToken,
            new { type = 0, operationData = "{\"secret\":true}" });

        var res = await SendAsync(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", strangerToken);

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

        var leaveRes = await SendAsync(HttpMethod.Delete, $"/api/parties/{partyId}", leaderToken);
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

        await SendAsync(HttpMethod.Delete, $"/api/parties/{partyId}", leaderToken);

        var res = await SendAsync(HttpMethod.Get, $"/api/parties/{partyId}", memberToken);
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

        var kickRes = await SendAsync(HttpMethod.Delete, $"/api/parties/{partyId}/members/{memberId}", leaderToken);
        Assert.AreEqual(HttpStatusCode.NoContent, kickRes.StatusCode);

        Assert.IsFalse(MembersOf(await GetPartyAsync(leaderToken, partyId)).Any(m => m.userId == memberId));
        // Access to the canvas op-log rode on party membership, so it's gone too.
        var opsRes = await SendAsync(HttpMethod.Get, $"/api/canvas/{canvasId}/operations", memberToken);
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

        var blockRes = await SendAsync(HttpMethod.Post, $"/api/users/{targetId}/block", leaderToken);
        Assert.AreEqual(HttpStatusCode.NoContent, blockRes.StatusCode);

        var inviteRes = await SendAsync(HttpMethod.Post, $"/api/parties/{partyId}/invites", leaderToken,
            new { invitedUserId = targetId.ToString() });
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
        await SendAsync(HttpMethod.Post, $"/api/users/{targetId}/block", leaderToken);

        var respondRes = await SendAsync(HttpMethod.Post, $"/api/invites/{inviteId}/respond", targetToken, new { accepted = true });
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
        var before = await SendAsync(HttpMethod.Get, $"/api/canvas/{secondCanvas}/operations", memberToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, before.StatusCode);

        var patchRes = await SendAsync(HttpMethod.Patch, $"/api/parties/{partyId}/canvas", leaderToken,
            new { canvasId = secondCanvas.ToString() });
        Assert.AreEqual(HttpStatusCode.NoContent, patchRes.StatusCode);

        var after = await SendAsync(HttpMethod.Get, $"/api/canvas/{secondCanvas}/operations", memberToken);
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

        var endRes = await SendAsync(HttpMethod.Post, $"/api/parties/{partyId}/end", leaderToken);
        Assert.AreEqual(HttpStatusCode.NoContent, endRes.StatusCode);

        var partyRes = await SendAsync(HttpMethod.Get, $"/api/parties/{partyId}", leaderToken);
        Assert.AreEqual(HttpStatusCode.NotFound, partyRes.StatusCode);

        var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/canvas", leaderToken));
        Assert.IsTrue(list.EnumerateArray().Any(c => IdOf(c) == canvasId));
    }

    // ─── Friends lifecycle ──────────────────────────────────────

    [TestMethod]
    public async Task Workflow_FriendRequestAccepted_BothListEachOther()
    {
        var (aId, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");

        var sendRes = await SendAsync(HttpMethod.Post, $"/api/friends/{bId}", aToken);
        Assert.AreEqual(HttpStatusCode.Created, sendRes.StatusCode);

        // B finds the incoming request and accepts it.
        var reqs = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/friend-requests", bToken));
        var incoming = reqs.EnumerateArray().Single(r => Guid.Parse(r.GetProperty("userId").GetString()!) == aId);
        var requestId = IdOf(incoming);

        var acceptRes = await SendAsync(HttpMethod.Patch, $"/api/friend-requests/{requestId}", bToken,
            new { requesterId = aId.ToString(), accepted = true });
        Assert.AreEqual(HttpStatusCode.OK, acceptRes.StatusCode);

        Assert.IsTrue(await FriendListContainsAsync(aToken, bId));
        Assert.IsTrue(await FriendListContainsAsync(bToken, aId));
    }

    [TestMethod]
    public async Task Workflow_FriendRequestRejected_NoFriendship()
    {
        var (aId, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");
        await SendAsync(HttpMethod.Post, $"/api/friends/{bId}", aToken);

        var reqs = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/friend-requests", bToken));
        var requestId = IdOf(reqs.EnumerateArray().Single(r => Guid.Parse(r.GetProperty("userId").GetString()!) == aId));

        var rejectRes = await SendAsync(HttpMethod.Patch, $"/api/friend-requests/{requestId}", bToken,
            new { requesterId = aId.ToString(), accepted = false });
        Assert.AreEqual(HttpStatusCode.OK, rejectRes.StatusCode);

        Assert.IsFalse(await FriendListContainsAsync(aToken, bId));
        Assert.IsFalse(await FriendListContainsAsync(bToken, aId));
    }

    [TestMethod]
    public async Task Workflow_SenderCancelsRequestThenResends_Succeeds()
    {
        var (_, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");

        var send1Body = await ReadJsonAsync(await SendAsync(HttpMethod.Post, $"/api/friends/{bId}", aToken));
        var requestId = IdOf(send1Body);

        var cancelRes = await SendAsync(HttpMethod.Delete, $"/api/friend-requests/{requestId}", aToken);
        Assert.AreEqual(HttpStatusCode.NoContent, cancelRes.StatusCode);

        // B's inbox no longer shows the cancelled request as pending (status enum
        // serializes as its numeric value; Pending == 0).
        var reqs = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/friend-requests", bToken));
        Assert.IsFalse(reqs.EnumerateArray().Any(r =>
            IdOf(r) == requestId && r.GetProperty("status").GetInt32() == 0));

        // And a brand-new request is allowed after the cancel.
        var send2 = await SendAsync(HttpMethod.Post, $"/api/friends/{bId}", aToken);
        Assert.AreEqual(HttpStatusCode.Created, send2.StatusCode);
    }

    [TestMethod]
    public async Task Workflow_UnfriendThenReRequest_Succeeds()
    {
        var (aId, aToken) = await NewUserAsync("aaa");
        var (bId, bToken) = await NewUserAsync("bbb");
        await SendAsync(HttpMethod.Post, $"/api/friends/{bId}", aToken);
        var reqs = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/friend-requests", bToken));
        var requestId = IdOf(reqs.EnumerateArray().Single(r => Guid.Parse(r.GetProperty("userId").GetString()!) == aId));
        await SendAsync(HttpMethod.Patch, $"/api/friend-requests/{requestId}", bToken, new { requesterId = aId.ToString(), accepted = true });

        var unfriendRes = await SendAsync(HttpMethod.Delete, $"/api/friends/{bId}", aToken);
        Assert.AreEqual(HttpStatusCode.NoContent, unfriendRes.StatusCode);
        Assert.IsFalse(await FriendListContainsAsync(aToken, bId));
        Assert.IsFalse(await FriendListContainsAsync(bToken, aId));

        // Nothing stops a fresh request after unfriending.
        var reSend = await SendAsync(HttpMethod.Post, $"/api/friends/{bId}", aToken);
        Assert.AreEqual(HttpStatusCode.Created, reSend.StatusCode);
    }

    // ─── Auth gate ──────────────────────────────────────────────

    [TestMethod]
    public async Task Workflow_ProtectedEndpointWithoutToken_Unauthorized()
    {
        var res = await SendAsync(HttpMethod.Get, "/api/canvas");
        Assert.AreEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
