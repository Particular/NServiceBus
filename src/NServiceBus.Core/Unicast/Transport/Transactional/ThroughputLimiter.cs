namespace NServiceBus.Unicast.Transport.Transactional
{
    using System;
    using System.Threading;
    using Logging;

    /// <summary>
    /// Support for throughput limitation of the transport
    /// </summary>
    public class ThroughputLimiter
    {
        public void Start(int limit)
        {
            if (limit == 0)
                return;

            througputSemaphore = new Semaphore(limit, limit);
            timer = new Timer(ResetLimit, null, 0, 1000);
        }

        public void MessageProcessed()
        {
            if (througputSemaphore == null)
                return;

            througputSemaphore.WaitOne();
            Interlocked.Increment(ref numberOfMessagesProcessed);
        }

        void ResetLimit(object state)
        {
            var numberOfMessagesProcessedSnapshot = Interlocked.Exchange(ref numberOfMessagesProcessed, 0);

            if (numberOfMessagesProcessedSnapshot > 0)
                througputSemaphore.Release((int)numberOfMessagesProcessedSnapshot);
        }

        long numberOfMessagesProcessed;
        Timer timer;
        Semaphore througputSemaphore;
    }
}