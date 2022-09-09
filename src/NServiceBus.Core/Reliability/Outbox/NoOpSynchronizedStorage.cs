namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class NoOpSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag) => NoOpSynchronizedStorageAdapter.EmptyResult;
    }
}