#nullable enable
namespace Inkboard.Application.Interfaces;

public interface IConnectionStore
{
    public void Add(string connectionId, Guid userId, string hubName);
    public void Remove(Guid userId, string hubName);
    public string? Get(Guid userId, string hubName);
    public void RegisterAbort(string connectionId, Guid userId, Action abort);
    public void UnregisterAbort(string connectionId);
    public Task DisconnectUserAsync(Guid userId);
}
