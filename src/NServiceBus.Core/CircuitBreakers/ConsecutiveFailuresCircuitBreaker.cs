#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class ConsecutiveFailuresCircuitBreaker(
    string name,
    int consecutiveFailuresBeforeTriggering,
    Func<long, CancellationToken, Task> triggerAction,
    Func<long, CancellationToken, Task> disarmAction,
    TimeSpan armedFailureDelayDuration)
{
    public Task Success(CancellationToken cancellationToken = default)
    {
        var oldValue = Interlocked.Exchange(ref failureCount, 0);

        if (oldValue < consecutiveFailuresBeforeTriggering)
        {
            return Task.CompletedTask;
        }

        Logger.InfoFormat("The circuit breaker for {0} is now disarmed", name);
        return disarmAction(DateTime.UtcNow.Ticks, cancellationToken);
    }

    public async Task Failure(CancellationToken cancellationToken = default)
    {
        if (consecutiveFailuresBeforeTriggering != int.MaxValue)
        {
            var newValue = Interlocked.Increment(ref failureCount);

            if (newValue == consecutiveFailuresBeforeTriggering)
            {
                Logger.WarnFormat("The circuit breaker for {0} is now in the armed state", name);
                await triggerAction(DateTime.UtcNow.Ticks, cancellationToken).ConfigureAwait(false);

                await Task.Delay(armedFailureDelayDuration, cancellationToken).ConfigureAwait(false);
            }
            else if (newValue > consecutiveFailuresBeforeTriggering)
            {
                await Task.Delay(armedFailureDelayDuration, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    int failureCount;

    static readonly ILog Logger = LogManager.GetLogger<ConsecutiveFailuresCircuitBreaker>();
}