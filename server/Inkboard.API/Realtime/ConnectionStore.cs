using System.Collections.Concurrent;
using Inkboard.Application.Interfaces;

namespace Inkboard.API.Realtime;

public class ConnectionStore : IConnectionStore
{
    private readonly ConcurrentDictionary<Guid, string> connections = new();

    public ConnectionStore()
    {
    }

    public void Add(string connectionId, Guid userId)
    {
        connections[userId] = connectionId;
    }

    public string? Get(Guid userId)
    {
        if (connections.TryGetValue(userId, out var connId))
        {
            return connId;
        }
        return null;
    }

    public void Remove(Guid userId)
    {
        connections.TryRemove(userId, out var _);
    }
}
