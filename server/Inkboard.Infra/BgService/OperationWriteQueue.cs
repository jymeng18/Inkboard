using System.Threading.Channels;
using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;

namespace Inkboard.Infra.BgService;

/// <summary>
/// Bounded in-memory queue of operations awaiting persistence. Enqueue is O(1) and
/// non-blocking; when db slow, the oldest pending op is
/// dropped rather than blocking the hub, since persistence is best effort here (the
/// live op already reached the other clients). The background worker reads Reader.
/// </summary>
public class OperationWriteQueue : IOperationWriteQueue
{
    // Channels pass data from one party to another by a Queue
    private readonly Channel<CanvasOperation> _channel = Channel.CreateBounded<CanvasOperation>(
        new BoundedChannelOptions(10_000) // max 10000 items
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        }
    );

    public ChannelReader<CanvasOperation> Reader => _channel.Reader;

    public void Enqueue(CanvasOperation operation)
    {
        _channel.Writer.TryWrite(operation);
    }
}
