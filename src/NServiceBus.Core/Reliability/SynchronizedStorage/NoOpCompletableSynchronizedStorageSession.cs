namespace NServiceBus
{
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;

    // Do not allow Fody to weave the IDisposable for us so that other threads can still access the instance of this class
    // even after it has been disposed.
    [SkipWeaving]
    sealed class NoOpCompletableSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public Task CompleteAsync() => TaskEx.CompletedTask;

        public void Dispose()
        {
        }

        public static readonly CompletableSynchronizedStorageSession Instance = new NoOpCompletableSynchronizedStorageSession();
    }
}