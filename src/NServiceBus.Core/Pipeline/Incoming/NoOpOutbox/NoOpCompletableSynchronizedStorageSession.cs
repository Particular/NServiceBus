namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;

    // Do not allow Fody to weave the IDisposable for us so that other threads can still access the instance of this class
    // even after it has been disposed.
    [SkipWeaving]
    class NoOpCompletableSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public Task CompleteAsync()
        {
            return TaskEx.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}