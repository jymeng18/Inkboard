using Inkboard.API.Hubs;
using Inkboard.API.Realtime;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Inkboard.Tests.Auth;

[TestClass]
public sealed class AccountConnectionTests
{
    private static ConnectionStore CreateStore(Mock<IPartyHubClient>? client = null)
    {
        client ??= new Mock<IPartyHubClient>();
        var clients = new Mock<IHubClients<IPartyHubClient>>();
        clients.Setup(hub => hub.User(It.IsAny<string>())).Returns(client.Object);
        var hub = new Mock<IHubContext<PartyHub, IPartyHubClient>>();
        hub.SetupGet(context => context.Clients).Returns(clients.Object);
        return new ConnectionStore(hub.Object);
    }

    [TestMethod]
    public async Task DisconnectUserAsync_WhenUserHasSeveralTabs_ClosesEveryConnection()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        var disconnected = new HashSet<string>();
        store.RegisterAbort("party-tab-1", userId, () => disconnected.Add("party-tab-1"));
        store.RegisterAbort("party-tab-2", userId, () => disconnected.Add("party-tab-2"));
        store.RegisterAbort("canvas-tab", userId, () => disconnected.Add("canvas-tab"));

        await store.DisconnectUserAsync(userId);

        CollectionAssert.AreEquivalent(new[] { "party-tab-1", "party-tab-2", "canvas-tab" }, disconnected.ToArray());
    }

    [TestMethod]
    public async Task DisconnectUserAsync_WhenAnotherUserIsConnected_KeepsTheirConnectionOpen()
    {
        var store = CreateStore();
        var otherUserDisconnected = false;
        store.RegisterAbort("other-user", Guid.NewGuid(), () => otherUserDisconnected = true);

        await store.DisconnectUserAsync(Guid.NewGuid());

        Assert.IsFalse(otherUserDisconnected);
    }

    [TestMethod]
    public async Task RegisterAbort_WhenAccountDeletedDuringHandshake_ClosesLateConnection()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        await store.DisconnectUserAsync(userId);
        var disconnected = false;

        store.RegisterAbort("late-connection", userId, () => disconnected = true);

        Assert.IsTrue(disconnected);
    }

    [TestMethod]
    public async Task DisconnectUserAsync_WhenNotificationFails_StillClosesConnection()
    {
        var client = new Mock<IPartyHubClient>();
        client.Setup(hub => hub.AccountDeleted()).ThrowsAsync(new IOException("Connection lost."));
        var store = CreateStore(client);
        var userId = Guid.NewGuid();
        var disconnected = false;
        store.RegisterAbort("connection", userId, () => disconnected = true);

        await Assert.ThrowsAsync<IOException>(() => store.DisconnectUserAsync(userId));

        Assert.IsTrue(disconnected);
    }
}
