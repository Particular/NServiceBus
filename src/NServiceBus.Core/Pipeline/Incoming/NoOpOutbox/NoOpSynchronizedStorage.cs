namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class NoOpSynchronizedStorage : ISynchronizedStorage
    {
        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            return NoOpSynchronizedStorageAdapter.EmptyResult;
        }
    }
}