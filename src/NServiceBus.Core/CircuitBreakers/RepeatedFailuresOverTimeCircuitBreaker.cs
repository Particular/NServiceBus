namespace NServiceBus.CircuitBreakers
{
    using System;
    using System.Threading;
    using Logging;

    /// <summary>
    /// A circuit breaker that triggers after a given time 
    /// </summary>
    public class RepeatedFailuresOverTimeCircuitBreaker : IDisposable
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timeToWaitBeforeTriggering"></param>
        /// <param name="triggerAction"></param>
        public RepeatedFailuresOverTimeCircuitBreaker(string name, TimeSpan timeToWaitBeforeTriggering,
            Action<Exception> triggerAction)
            : this(name, timeToWaitBeforeTriggering, triggerAction, TimeSpan.FromSeconds(1))
        {
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timeToWaitBeforeTriggering"></param>
        /// <param name="triggerAction"></param>
        /// <param name="delayAfterFailure"></param>
        public RepeatedFailuresOverTimeCircuitBreaker(string name, TimeSpan timeToWaitBeforeTriggering,
            Action<Exception> triggerAction, TimeSpan delayAfterFailure)
        {
            Guard.AgainstNullAndEmpty(name, "name");
            Guard.AgainstNull(triggerAction, "triggerAction");
            Guard.AgainstNegative(timeToWaitBeforeTriggering, "delayAfterFailure");
            Guard.AgainstNegative(delayAfterFailure, "delayAfterFailure");
            this.name = name;
            this.delayAfterFailure = delayAfterFailure;
            this.triggerAction = triggerAction;
            this.timeToWaitBeforeTriggering = timeToWaitBeforeTriggering;

            timer = new Timer(CircuitBreakerTriggered);
        }

        /// <summary>
        /// Tell the CB that it should disarm
        /// </summary>
        public bool Success()
        {
            var oldValue = Interlocked.Exchange(ref failureCount, 0);

            if (oldValue == 0)
            {
                return false;
            }

            timer.Change(Timeout.Infinite, Timeout.Infinite);
            Logger.InfoFormat("The circuit breaker for {0} is now disarmed", name);

            return true;
        }

        /// <summary>
        /// Tells the CB to arm
        /// </summary>
        /// <param name="exception"></param>
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

        /// <summary>
        /// Disposes the CB
        /// </summary>
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
        static ILog Logger = LogManager.GetLogger<RepeatedFailuresOverTimeCircuitBreaker>();

        readonly TimeSpan delayAfterFailure;
        readonly string name;
        TimeSpan timeToWaitBeforeTriggering;
        Timer timer;
        readonly Action<Exception> triggerAction;
        long failureCount;
        Exception lastException;
    }
}