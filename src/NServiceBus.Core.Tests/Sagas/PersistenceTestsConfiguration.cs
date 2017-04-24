namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Gateway.Deduplication;
    using NUnit.Framework;
    using Outbox;
    using Sagas;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration()
        {
            storageLocation = Path.Combine(TestContext.CurrentContext.TestDirectory, ".sagas");
            var sagaManifests = new SagaManifestCollection(SagaMetadataCollection, storageLocation);
            SynchronizedStorage = new LearningSynchronizedStorage(sagaManifests);
            SynchronizedStorageAdapter = new LearningStorageAdapter();
            SagaStorage = new LearningSagaPersister();
            var sagaIdGenerator = new LearningSagaIdGenerator();
            SagaIdGenerator = sagaIdGenerator;
        }

        public bool SupportsDtc { get; } = false;
        public bool SupportsOutbox { get; } = false;
        public bool SupportsFinders { get; } = false;
        public bool SupportsSubscriptions { get; } = false;
        public bool SupportsTimeouts { get; } = false;
        public ISagaIdGenerator SagaIdGenerator { get; }

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
            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            Directory.Delete(storageLocation, true);
            return Task.FromResult(0);
        }

        public Task CleanupMessagesOlderThan(DateTimeOffset beforeStore)
        {
            return Task.FromResult(0);
        }

        string storageLocation;
    }
}