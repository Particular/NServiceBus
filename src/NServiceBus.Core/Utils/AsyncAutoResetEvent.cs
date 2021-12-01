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
            Task result;
            lock (mutex)
            {
                if (signaled)
                {
                    signaled = false;
                    result = TaskEx.TrueTask;
                }
                else
                {
                    result = signalQueue.Enqueue(mutex, cancellationToken);
                }
            }

            return result;
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
