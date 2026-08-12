using System.Collections.Concurrent;
using Inkboard.API.Hubs;
using Inkboard.Application.Interfaces;

namespace Inkboard.API.Realtime;

public class ConnectionStore : IConnectionStore
{
    private readonly ConcurrentDictionary<HubUserKey, string> connections = new();

    public ConnectionStore()
    {   
    }

    public void Add(string connectionId, Guid userId, string hubName)
    {
        var key = new HubUserKey(userId, nameof(hubName));
        connections[key] = connectionId;
    }

    public string? Get(Guid userId, string hubName)
    {
        var key = new HubUserKey(userId, nameof(hubName));
        if (connections.TryGetValue(key, out var connId))
        {
            return connId;
        }
        return null;
    }

    public void Remove(Guid userId, string hubName)
    {
        var key = new HubUserKey(userId, nameof(hubName));
        connections.TryRemove(key, out var _);
    }
}
