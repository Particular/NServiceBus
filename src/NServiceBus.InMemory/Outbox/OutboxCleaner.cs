#nullable enable

namespace NServiceBus.Persistence.InMemory;

using System;
using System.Threading;
using System.Threading.Tasks;
using Features;

/// <summary>
/// Background task that periodically cleans up old dispatched outbox entries.
/// Uses PeriodicTimer for async-friendly periodic execution.
/// </summary>
class OutboxCleaner(InMemoryOutboxStorage storage, TimeSpan timeToKeepDeduplicationData) : FeatureStartupTask
{
    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
    {
        // Use PeriodicTimer for async-friendly periodic execution
        cleanupTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        cleanupTask = RunCleanupLoopAsync(cleanupTimer, cancellationToken);
        return Task.CompletedTask;
    }

    protected override async Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
    {
        if (cleanupTimer is null)
        {
            return;
        }

        // Signal the timer to stop
        cleanupTimer.Dispose();

        // Wait for the cleanup loop to complete (with timeout for safety)
        if (cleanupTask is not null)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                await cleanupTask.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                // Timeout or cancellation requested, proceed with shutdown
            }
        }

        cleanupTimer = null;
        cleanupTask = null;
    }

    async Task RunCleanupLoopAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                PerformCleanup();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation requested, exit gracefully
        }
    }

    void PerformCleanup()
    {
        try
        {
            storage.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
        }
        catch
        {
            // Ignore exceptions during cleanup - don't crash the timer loop
        }
    }

    PeriodicTimer? cleanupTimer;
    Task? cleanupTask;
}
