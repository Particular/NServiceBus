namespace NServiceBus
{
    using System;
    using System.Threading;

    class CircuitBreaker : IDisposable
    {
        public CircuitBreaker(int threshold, TimeSpan resetEvery)
        {
            this.threshold = threshold;
            timer = new Timer(state => failureCount = 0, null, resetEvery, resetEvery);
        }

        public void Dispose()
        {
            //Injected
        }

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

        int failureCount;
        int firedTimes;
        int threshold;
#pragma warning disable IDE0052 // Remove unread private members
        Timer timer;
#pragma warning restore IDE0052 // Remove unread private members
    }
}