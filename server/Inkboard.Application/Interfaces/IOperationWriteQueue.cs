using Inkboard.Domain.Models;

namespace Inkboard.Application.Interfaces;

/// <summary>
/// Accepts finished canvas operations for persistence without blocking the caller.
/// Enqueue is non-blocking and fire-and-forget: a background worker drains and
/// batch-inserts, so the real-time broadcast path never waits on the database.
/// </summary>
public interface IOperationWriteQueue
{
    void Enqueue(CanvasOperation operation);
}
