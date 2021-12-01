namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    // Taken from https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Coordination/AsyncWaitQueue.cs
    // Can't use the package because it is net462 and this currently targets 452
    class AsyncWaitQueue<T>
    {
        public Task<T> Enqueue(object mutex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<T>();
#if NETSTANDARD2_0_OR_GREATER
                tcs.TrySetCanceled(cancellationToken);
#else
                tcs.TrySetCanceled();
#endif
                return tcs.Task;
            }

            var ret = Enqueue();
            if (!cancellationToken.CanBeCanceled)
            {
                return ret;
            }

            var registration = cancellationToken.Register(() =>
            {
                lock (mutex)
                {
                    TryCancel(ret);
                }
            }, useSynchronizationContext: false);

            return (Task<T>)ret.ContinueWith(_ => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public void Dequeue(T result)
        {
            _queue.RemoveFromFront().TrySetResult(result);
        }

        public bool IsEmpty => _queue.Count == 0;

        Task<T> Enqueue()
        {
            var tcs = new TaskCompletionSource<T>();
            _queue.AddToBack(tcs);
            return tcs.Task;
        }

        bool TryCancel(Task task)
        {
            for (int i = 0; i != _queue.Count; ++i)
            {
                if (_queue[i].Task == task)
                {
                    _queue[i].TrySetCanceled();
                    _queue.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        readonly Deque<TaskCompletionSource<T>> _queue = new Deque<TaskCompletionSource<T>>();
    }
}