namespace NServiceBus.AcceptanceTesting
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class AcceptanceTestingSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken)
        {
            var session = (CompletableSynchronizedStorageSession)new AcceptanceTestingSynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}