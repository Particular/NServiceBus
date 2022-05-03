namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false;

        public bool SupportsOutbox => false;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency => true;

        public ISagaIdGenerator SagaIdGenerator { get; private set; }

        public ISagaPersister SagaStorage { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

        public Task Configure(CancellationToken cancellationToken = default)
        {
            SagaIdGenerator = new LearningSagaIdGenerator();
            SagaStorage = new LearningSagaPersister();

            var sagaManifests = new SagaManifestCollection(SagaMetadataCollection,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".sagas"),
                name => DeterministicGuid.Create(name).ToString());

            CreateStorageSession = () => new LearningSynchronizedStorageSession(sagaManifests);

            return Task.CompletedTask;
        }

        public Task Cleanup(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}