namespace NServiceBus.CircuitBreakers
{
    using System;
    using System.Threading;

    /// <summary>
    /// A circuit breaker implementation.
    /// </summary>
    class CircuitBreaker : IDisposable
    {
        int threshold;
        int firedTimes;
        // ReSharper disable once NotAccessedField.Local
        Timer timer;
        int failureCount;

        /// <summary>
        /// Create a <see cref="CircuitBreaker"/>.
        /// </summary>
        /// <param name="threshold">Number of triggers before it fires.</param>
        /// <param name="resetEvery">The <see cref="TimeSpan"/> to wait before resetting the <see cref="CircuitBreaker"/>.</param>
        public CircuitBreaker(int threshold, TimeSpan resetEvery)
        {
            this.threshold = threshold;
            timer = new Timer(state => failureCount = 0, null, resetEvery, resetEvery);
        }

        /// <summary>
        /// Method to execute.
        /// </summary>
        /// <param name="trigger">The callback to execute.</param>
        public void Execute(Action trigger)
        {
            if (Interlocked.Increment(ref failureCount) > threshold)
            {
                if (Interlocked.Exchange(ref firedTimes, 1) == 0)
                {
                    trigger();
                }
            }
        }

        public void Dispose()
        {
            //Injected
        }
    }
}