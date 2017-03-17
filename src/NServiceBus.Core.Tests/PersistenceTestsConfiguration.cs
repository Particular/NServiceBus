namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Gateway.Deduplication;
    using Outbox;
    using Sagas;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration()
        {
            var storageLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "componenttests");
            var allSagas = new SagaMetadataCollection();

            allSagas.Initialize(new List<Type>
            {
                typeof(SagaWithCorrelationProperty),
               // typeof(SagaWithoutCorrelationProperty),
                typeof(AnotherSagaWithCorrelatedProperty),
               // typeof(AnotherSagaWithoutCorrelationProperty),
                typeof(SimpleSagaEntitySaga),
                typeof(TestSaga)
            });

            var sagaManifests = new SagaManifestCollection(allSagas, storageLocation);

            SagaStorage = new DevelopmentSagaPersister(sagaManifests);

            SynchronizedStorage = new DevelopmentSyncronizedStorage();

            SagaIdGenerator = new DevelopmentSagaIdGenerator();
        }

        public bool SupportsDtc { get; } = false;
        public ISagaPersister SagaStorage { get; }
        public ISynchronizedStorage SynchronizedStorage { get; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }
        public ISubscriptionStorage SubscriptionStorage { get; }
        public IPersistTimeouts TimeoutStorage { get; }
        public IQueryTimeouts TimeoutQuery { get; }
        public IOutboxStorage OutboxStorage { get; }
        public IDeduplicateMessages GatewayStorage { get; }

        public ISagaIdGenerator SagaIdGenerator { get; }
        public Task Configure()
        {
            return TaskEx.CompletedTask;
        }

        public Task Cleanup()
        {
            return TaskEx.CompletedTask;
        }
    }
}