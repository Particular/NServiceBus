namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    // Taken from https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-2-asyncautoresetevent/
    // Can't use the package because it is net462 and this currently targets 452
    class AsyncAutoResetEvent
    {
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            lock (mutex)
            {
                if (signaled)
                {
                    signaled = false;
                    return Task.FromResult(true);
                }
                else
                {
                    return signalQueue.Enqueue(mutex, cancellationToken);
                }
            }
        }

        public void Set()
        {
            lock (mutex)
            {
                if (signalQueue.IsEmpty)
                {
                    signaled = true;
                }
                else
                {
                    signalQueue.Dequeue(true);
                }
            }
        }

        readonly AsyncWaitQueue<object> signalQueue = new AsyncWaitQueue<object>();
        readonly object mutex = new object();
        bool signaled;
    }
}
