namespace NServiceBus.Utils
{
    using System;
    using System.Threading;

    internal class CircuitBreaker
    {
        private readonly int threashold;
        private int firedTimes;
        private Timer timer;
        private int failureCount;

        public CircuitBreaker(int threashold, TimeSpan resetEvery)
        {
            this.threashold = threashold;
            timer = new Timer(state => failureCount = 0, null, resetEvery, resetEvery);
        }

        public void Execute(Action trigger)
        {
            if (Interlocked.Increment(ref failureCount) > threashold)
            {
                if (Interlocked.Exchange(ref firedTimes, 1) == 0)
                {
                    trigger();
                }
            }
        }
    }
}