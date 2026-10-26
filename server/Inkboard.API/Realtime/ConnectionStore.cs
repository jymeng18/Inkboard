using System.Collections.Concurrent;
using Inkboard.API.Hubs;
using Inkboard.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Inkboard.API.Realtime;

public class ConnectionStore : IConnectionStore
{
    private readonly ConcurrentDictionary<HubUserKey, string> connections = new();
    private readonly ConcurrentDictionary<string, (Guid UserId, Action Abort)> aborts = new();
    private readonly ConcurrentDictionary<Guid, byte> deletedUsers = new();
    private readonly IHubContext<PartyHub, IPartyHubClient> _partyHub;

    public ConnectionStore(IHubContext<PartyHub, IPartyHubClient> partyHub)
    {
        _partyHub = partyHub;
    }

    public void Add(string connectionId, Guid userId, string hubName)
    {
        var key = new HubUserKey(userId, hubName);
        connections[key] = connectionId;
    }

    public string? Get(Guid userId, string hubName)
    {
        var key = new HubUserKey(userId, hubName);
        if (connections.TryGetValue(key, out var connId))
        {
            return connId;
        }
        return null;
    }

    public void Remove(Guid userId, string hubName)
    {
        var key = new HubUserKey(userId, hubName);
        connections.TryRemove(key, out var _);
    }

    public void RegisterAbort(string connectionId, Guid userId, Action abort)
    {
        aborts[connectionId] = (userId, abort);
        // A handshake can finish after its account was deleted. Close that
        // socket too, even if it missed the disconnect sweep.
        if (deletedUsers.ContainsKey(userId) && aborts.TryRemove(connectionId, out var removed))
            removed.Abort();
    }

    public void UnregisterAbort(string connectionId)
    {
        aborts.TryRemove(connectionId, out _);
    }

    public async Task DisconnectUserAsync(Guid userId)
    {
        deletedUsers[userId] = 0;
        try
        {
            await _partyHub.Clients.User(userId.ToString()).AccountDeleted();
        }
        finally
        {
            // Keep an abort handle per connection, including multiple tabs on
            // the same hub. No authenticated socket may outlive its account.
            foreach (var connection in aborts)
            {
                if (connection.Value.UserId == userId && aborts.TryRemove(connection.Key, out var removed))
                    removed.Abort();
            }

            foreach (var key in connections.Keys)
            {
                if (key.UserId == userId)
                    connections.TryRemove(key, out _);
            }
        }
    }
}
