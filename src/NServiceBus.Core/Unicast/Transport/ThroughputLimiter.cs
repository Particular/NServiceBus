namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Threading;

    /// <summary>
    ///     Support for throughput limitation of the transport
    /// </summary>
    class ThroughputLimiter:IDisposable
    {
        public void Start(int limit)
        {
            if (limit <= 0)
            {
                return;
            }

            numberOfMessagesProcessed = 0;
            throughputSemaphore = new SemaphoreSlim(limit, limit);
            cancellationTokenSource = new CancellationTokenSource();
            timer = new Timer(ResetLimit, null, 1000, 1000);
            started = true;
        }

        public void Stop()
        {
            if (!started)
            {
                return;
            }

            started = false;

            using (var waitHandle = new ManualResetEvent(false))
            {
                timer.Dispose(waitHandle);

                waitHandle.WaitOne();
            }

            cancellationTokenSource.Cancel();

            BlockUntilZeroMessagesBeingProcessed();

            throughputSemaphore.Dispose();
            cancellationTokenSource.Dispose();
        }

        void BlockUntilZeroMessagesBeingProcessed()
        {
            while (numberOfMessagesProcessing > 0)
            {
                Thread.SpinWait(5);
            }
        }

        public void MessageProcessed()
        {
            if (!started)
            {
                return;
            }

            try
            {
                Interlocked.Increment(ref numberOfMessagesProcessing);
                throughputSemaphore.Wait(cancellationTokenSource.Token);
                Interlocked.Increment(ref numberOfMessagesProcessed);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Interlocked.Decrement(ref numberOfMessagesProcessing);
            }
        }

        void ResetLimit(object state)
        {
            var numberOfMessagesProcessedSnapshot = Interlocked.Exchange(ref numberOfMessagesProcessed, 0);

            if (numberOfMessagesProcessedSnapshot > 0)
            {
                throughputSemaphore.Release(numberOfMessagesProcessedSnapshot);
            }
        }

        CancellationTokenSource cancellationTokenSource;
        int numberOfMessagesProcessed;
        int numberOfMessagesProcessing;
        bool started;
        SemaphoreSlim throughputSemaphore;
        Timer timer;

        public void Dispose()
        {
            //Injected
        }
        void DisposeManaged()
        {
            Stop();
        }
    }
}