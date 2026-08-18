using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inkboard.Infra.BgService;

/// <summary>
/// Drains the operation write queue and bulk-inserts in batches, flushing when a
/// batch fills or a short window elapses after the first op.
/// </summary>
public class OperationPersistenceWorker : BackgroundService
{
    private const int MaxBatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly OperationWriteQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationPersistenceWorker> _logger;

    public OperationPersistenceWorker(
        OperationWriteQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<OperationPersistenceWorker> logger
    )
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ExecuteAsync needs to listen to two different CancellationTokens, one from Host, one from 'window'
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _queue.Reader;
        var batch = new List<CanvasOperation>(MaxBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            CanvasOperation first;
            try
            {
                // Initial check to see if there exists any data to be read
                first = await reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            batch.Clear();
            batch.Add(first);

            // See if we can get any more ops within FlushInterval, if not just flush and save
            // stoppingToken can stop both listeners, but window CTS can only stop its own
            using var window = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            window.CancelAfter(FlushInterval);
            try
            {
                while (batch.Count < MaxBatchSize)
                {
                    batch.Add(await reader.ReadAsync(window.Token));
                }
            }
            catch (OperationCanceledException)
            {
                // not actually any bug, just something ReadAsync(..) will throw when Cancellation requests execute
            }

            await FlushAsync(batch);
        }
    }

    private async Task FlushAsync(List<CanvasOperation> batch)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOperationRepository>();
            await repository.AddRangeAsync(batch);
        }
        catch (Exception ex)
        {
            // Best effort: a failed persist must not take the worker down.
            _logger.LogError(ex, "Failed to persist {Count} canvas operations.", batch.Count);
        }
    }
}
