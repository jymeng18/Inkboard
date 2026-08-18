#nullable enable
namespace Inkboard.Application.Interfaces;

public interface IConnectionStore
{
    public void Add(string connectionId, Guid userId, string hubName);
    public void Remove(Guid userId, string hubName);
    public string? Get(Guid userId, string hubName);
}
