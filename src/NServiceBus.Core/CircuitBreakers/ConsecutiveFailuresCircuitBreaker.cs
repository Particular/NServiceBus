namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    class ConsecutiveFailuresCircuitBreaker : IDisposable
    {
        public ConsecutiveFailuresCircuitBreaker(string name, int consecutiveFailuresBeforeTriggering, Func<long, CancellationToken, Task> triggerAction, Func<long, CancellationToken, Task> disarmAction, TimeSpan armedFailureDelayDuration)
        {
            this.name = name;
            this.triggerAction = triggerAction;
            this.consecutiveFailuresBeforeTriggering = consecutiveFailuresBeforeTriggering;
            this.disarmAction = disarmAction;
            this.armedFailureDelayDuration = armedFailureDelayDuration;
        }

        public Task Success(CancellationToken cancellationToken = default)
        {
            var oldValue = Interlocked.Exchange(ref failureCount, 0);

            if (oldValue >= consecutiveFailuresBeforeTriggering)
            {
                Logger.InfoFormat("The circuit breaker for {0} is now disarmed", name);
                return disarmAction(DateTime.UtcNow.Ticks, cancellationToken);
            }

            return Task.CompletedTask;
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

        public void Dispose()
        {
            //Injected
        }

        int failureCount;
        string name;
        int consecutiveFailuresBeforeTriggering;
        Func<long, CancellationToken, Task> triggerAction;
        Func<long, CancellationToken, Task> disarmAction;
        TimeSpan armedFailureDelayDuration;

        static ILog Logger = LogManager.GetLogger<ConsecutiveFailuresCircuitBreaker>();
    }
}