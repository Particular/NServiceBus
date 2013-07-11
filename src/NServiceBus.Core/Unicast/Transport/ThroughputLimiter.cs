namespace NServiceBus.Unicast.Transport
{
    using System.Threading;

    /// <summary>
    /// Support for throughput limitation of the transport
    /// </summary>
    internal class ThroughputLimiter
    {
        public void Start(int limit)
        {
            if (limit <= 0)
                return;

            througputSemaphore = new SemaphoreSlim(limit, limit);
            timer = new Timer(ResetLimit, null, 0, 1000);
        }

        public void Stop()
        {
            if (througputSemaphore == null)
            {
                return;
            }

            timer.Dispose();

            stopResetEvent.WaitOne();

            througputSemaphore.Dispose();
            througputSemaphore = null;
        }

        public void MessageProcessed()
        {
            if (througputSemaphore == null)
                return;

            througputSemaphore.Wait();
            Interlocked.Increment(ref numberOfMessagesProcessed);
        }

        void ResetLimit(object state)
        {
            stopResetEvent.Reset();

            var numberOfMessagesProcessedSnapshot = Interlocked.Exchange(ref numberOfMessagesProcessed, 0);

            if (numberOfMessagesProcessedSnapshot > 0)
            {
                througputSemaphore.Release((int) numberOfMessagesProcessedSnapshot);
            }

            stopResetEvent.Set();
        }

        readonly ManualResetEvent stopResetEvent = new ManualResetEvent(true);
        Timer timer;
        long numberOfMessagesProcessed;
        SemaphoreSlim througputSemaphore;
    }
}