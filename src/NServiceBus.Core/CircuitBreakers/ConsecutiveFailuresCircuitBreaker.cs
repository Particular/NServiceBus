namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    class ConsecutiveFailuresCircuitBreaker : IDisposable, ICircuitBreaker
    {
        public ConsecutiveFailuresCircuitBreaker(string name, int consecutiveFailuresBeforeTriggering, Func<long, Task> triggerAction, Func<long, Task> disarmAction, TimeSpan armedFailureDelayDuration)
        {
            this.name = name;
            this.triggerAction = triggerAction;
            this.consecutiveFailuresBeforeTriggering = consecutiveFailuresBeforeTriggering;
            this.disarmAction = disarmAction;
            this.armedFailureDelayDuration = armedFailureDelayDuration;
        }

        public void Success()
        {
            var oldValue = Interlocked.Exchange(ref failureCount, 0);

            if (oldValue >= consecutiveFailuresBeforeTriggering)
            {
                Logger.InfoFormat("The circuit breaker for {0} is now disarmed", name);
                disarmAction(DateTime.UtcNow.Ticks);
            }
        }

        public Task Failure(Exception exception)
        {
            if (consecutiveFailuresBeforeTriggering != int.MaxValue)
            {
                var newValue = Interlocked.Increment(ref failureCount);

                if (newValue == consecutiveFailuresBeforeTriggering)
                {
                    Logger.WarnFormat("The circuit breaker for {0} is now in the armed state", name);
                    triggerAction(DateTime.UtcNow.Ticks);

                    return Task.Delay(armedFailureDelayDuration);
                }
                else if (newValue > consecutiveFailuresBeforeTriggering)
                {
                    return Task.Delay(armedFailureDelayDuration);
                }
            }

            return TaskEx.CompletedTask;
        }

        public void Dispose()
        {
            //Injected
        }

        int failureCount;
        string name;
        int consecutiveFailuresBeforeTriggering;
        Func<long, Task> triggerAction;
        Func<long, Task> disarmAction;
        TimeSpan armedFailureDelayDuration;

        static ILog Logger = LogManager.GetLogger<ConsecutiveFailuresCircuitBreaker>();
    }
}