namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
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
            var storageLocation = Environment.CurrentDirectory;
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

            var sagaManifests = new Dictionary<Type, SagaManifest>();

            foreach (var metadata in allSagas)
            {
                var sagaStorageDir = Path.Combine(storageLocation, metadata.SagaType.FullName.Replace("+", ""));

                if (!Directory.Exists(sagaStorageDir))
                {
                    Directory.CreateDirectory(sagaStorageDir);
                }

                var manifest = new SagaManifest
                {
                    StorageDirectory = sagaStorageDir,
                    Serializer = new DataContractJsonSerializer(metadata.SagaEntityType)
                };

                sagaManifests[metadata.SagaEntityType] = manifest;
            }

            SagaStorage = new DevelopmentSagaPersister(sagaManifests);

            SynchronizedStorage = new DevelopmentSyncronizedStorage();
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