namespace NServiceBus.AcceptanceTesting
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class AcceptanceTestingSynchronizedStorage : ISynchronizedStorage
    {
        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            var session = (ICompletableSynchronizedStorageSession)new AcceptanceTestingSynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}