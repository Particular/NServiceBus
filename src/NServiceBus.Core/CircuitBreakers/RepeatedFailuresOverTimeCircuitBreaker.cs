namespace NServiceBus.CircuitBreakers
{
    using System;
    using System.Threading;
    using Logging;

    public class RepeatedFailuresOverTimeCircuitBreaker : ICircuitBreaker, IDisposable
    {
        public RepeatedFailuresOverTimeCircuitBreaker(string name, TimeSpan timeToWaitBeforeTriggering,
            Action<Exception> triggerAction)
            : this(name, timeToWaitBeforeTriggering, triggerAction, TimeSpan.FromSeconds(1))
        {
        }

        public RepeatedFailuresOverTimeCircuitBreaker(string name, TimeSpan timeToWaitBeforeTriggering,
            Action<Exception> triggerAction, TimeSpan delayAfterFailure)
        {
            this.name = name;
            this.delayAfterFailure = delayAfterFailure;
            this.triggerAction = triggerAction;
            this.timeToWaitBeforeTriggering = timeToWaitBeforeTriggering;

            timer = new Timer(CircuitBreakerTriggered);
        }

        public bool Success()
        {
            var newValue = Interlocked.Exchange(ref failureCount, 0);

            if (newValue == 0)
            {
                return false;
            }

            timer.Change(Timeout.Infinite, Timeout.Infinite);
            Logger.InfoFormat("The circuit breaker for {0} is now disarmed", name);

            return true;
        }

        public void Failure(Exception exception)
        {
            lastException = exception;
            var newValue = Interlocked.Increment(ref failureCount);

            if (newValue == 1)
            {
                timer.Change(timeToWaitBeforeTriggering, NoPeriodicTriggering);
                Logger.InfoFormat("The circuit breaker for {0} is now in the armed state", name);
            }


            Thread.Sleep(delayAfterFailure);
        }

        public void Dispose()
        {
            //Injected
        }

        void CircuitBreakerTriggered(object state)
        {
            if (Interlocked.Read(ref failureCount) > 0)
            {
                Logger.WarnFormat("The circuit breaker for {0} will now be triggered", name);
                triggerAction(lastException);
            }
        }

        static readonly TimeSpan NoPeriodicTriggering = TimeSpan.FromMilliseconds(-1);
        static readonly ILog Logger = LogManager.GetLogger(typeof(RepeatedFailuresOverTimeCircuitBreaker));

        readonly TimeSpan delayAfterFailure;
        readonly string name;
        TimeSpan timeToWaitBeforeTriggering;
        Timer timer;
        readonly Action<Exception> triggerAction;
        long failureCount;
        Exception lastException;
    }
}