namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class LearningSynchronizedStorage : ISynchronizedStorage
    {
        public LearningSynchronizedStorage(SagaManifestCollection sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }

        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ICompletableSynchronizedStorageSession>(new LearningSynchronizedStorageSession(sagaManifests));
        }

        SagaManifestCollection sagaManifests;
    }
}