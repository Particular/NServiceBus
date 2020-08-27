namespace NServiceBus.TransportTests
{
    using System.Collections.Concurrent;

    static class ConcurrentQueueExtensions
    {
        public static void Clear(this ConcurrentQueue<TransportTestLoggerFactory.LogItem> queue)
        {
            while (!queue.IsEmpty)
            {
                queue.TryDequeue(out _);
            }
        }
    }
}