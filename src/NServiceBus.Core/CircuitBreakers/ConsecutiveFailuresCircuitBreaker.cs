namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    class ConsecutiveFailuresCircuitBreaker : IDisposable, ICircuitBreaker
    {
        public ConsecutiveFailuresCircuitBreaker(string name, long consecutiveFailuresBeforeTriggering, Action<Exception, long> triggerAction, Action<long> disarmAction)
        {
            this.name = name;
            this.triggerAction = triggerAction;
            this.consecutiveFailuresBeforeTriggering = consecutiveFailuresBeforeTriggering;
            this.disarmAction = disarmAction;
        }

        public void Success()
        {
            var oldValue = Interlocked.Exchange(ref failureCount, 0);

            if (oldValue >= consecutiveFailuresBeforeTriggering)
            {
                Logger.InfoFormat("The circuit breaker for {0} is now disarmed", name);
                disarmAction(DateTime.Now.Ticks);
            }
        }

        public Task Failure(Exception exception)
        {
            var newValue = Interlocked.Increment(ref failureCount);

            if (newValue >= consecutiveFailuresBeforeTriggering)
            {
                Logger.WarnFormat("The circuit breaker for {0} is now in the armed state", name);
                triggerAction(exception, DateTime.Now.Ticks);
            }

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            //Injected
        }

        long failureCount;
        string name;
        long consecutiveFailuresBeforeTriggering;
        Action<Exception, long> triggerAction;
        Action<long> disarmAction;

        static ILog Logger = LogManager.GetLogger<ConsecutiveFailuresCircuitBreaker>();
    }
}