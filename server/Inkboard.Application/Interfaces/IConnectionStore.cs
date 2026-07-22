#nullable enable
namespace Inkboard.Application.Interfaces;

public interface IConnectionStore
{
    public void Add(string connectionId, Guid userId);
    public void Remove(Guid userId);
    public string? Get(Guid userId);
}
