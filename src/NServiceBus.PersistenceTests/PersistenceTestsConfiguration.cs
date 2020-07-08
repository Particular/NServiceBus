#pragma warning disable 1591
namespace NServiceBus.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Features;
    using Outbox;
    using Persistence;
    using Sagas;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration : IPersistenceTestsConfiguration
    {
        public Func<ContextBag> GetContextBagForTimeoutPersister { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSagaStorage { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForOutbox { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSubscriptions { get; set; } = () => new ContextBag();

        public SagaMetadataCollection SagaMetadataCollection
        {
            get{
                if (sagaMetadataCollection == null)
                {
                    // TODO: look how mongo db does scanning for this
                    sagaMetadataCollection = new SagaMetadataCollection();
                    sagaMetadataCollection.Initialize(new[]
                    {
                        typeof(TestSaga)
                    });
                }
                return sagaMetadataCollection;
            }
            set { sagaMetadataCollection = value; }
        }

        SagaMetadataCollection sagaMetadataCollection;

    }
    
    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false; // TODO: verify if this is true
        public bool SupportsOutbox => true;
        public bool SupportsFinders => false;
        public bool SupportsSubscriptions => true;
        public bool SupportsTimeouts => true;
        public bool SupportsOptimisticConcurrency => true;
        public bool SupportsPessimisticConcurrency => false;
        public ISagaIdGenerator SagaIdGenerator => new DefaultSagaIdGenerator();
        public ISagaPersister SagaStorage => new InMemorySagaPersister();
        public ISynchronizedStorage SynchronizedStorage => new InMemorySynchronizedStorage();
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter => new InMemoryTransactionalSynchronizedStorageAdapter();
        public ISubscriptionStorage SubscriptionStorage => new InMemorySubscriptionStorage();
        public IPersistTimeouts TimeoutStorage => new InMemoryTimeoutPersister(() => DateTime.UtcNow); // todo: verify
        public IQueryTimeouts TimeoutQuery => new InMemoryTimeoutPersister(() => DateTime.Now);
        public IOutboxStorage OutboxStorage => new InMemoryOutboxStorage();
        public Task Configure()
        {
            return TaskEx.CompletedTask;
        }

        public Task Cleanup()
        {
            return TaskEx.CompletedTask;
        }

        public Task CleanupMessagesOlderThan(DateTimeOffset beforeStore)
        {
            return TaskEx.CompletedTask;
        }
    }
    
}