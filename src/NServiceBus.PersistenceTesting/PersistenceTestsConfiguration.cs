namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
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
            get
            {
               return sagaMetadataCollection;
            }
            set { sagaMetadataCollection = value; }
        }

        SagaMetadataCollection sagaMetadataCollection;
    }

    public partial class PersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration(TimeSpan? fromMilliseconds = null)
        {
           throw new InvalidOperationException("Run tests from NServiceBus.InMemory.PersistenceTests");
        }

        public bool SupportsDtc => false;
        public bool SupportsOutbox => true;
        public bool SupportsFinders => true;
        public bool SupportsSubscriptions => true;
        public bool SupportsTimeouts => true;
        public bool SupportsOptimisticConcurrency => true;
        public bool SupportsPessimisticConcurrency => false;
        public ISagaIdGenerator SagaIdGenerator { get; }
        public ISagaPersister SagaStorage  { get; }
        public ISynchronizedStorage SynchronizedStorage { get; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter  { get; }
        public ISubscriptionStorage SubscriptionStorage { get; }
        public IPersistTimeouts TimeoutStorage  { get; }
        public IQueryTimeouts TimeoutQuery { get; }
        public IOutboxStorage OutboxStorage  { get; }

        public Task Configure()
        {
            throw new NotImplementedException();
        }
        public Task Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}