namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false; // TODO: verify if this is true
        public bool SupportsOutbox => true;
        public bool SupportsFinders => true;  // TODO: verify if we actually need this as we think it should only be invoked by core
        public bool SupportsSubscriptions => true;
        public bool SupportsTimeouts => true;
        public bool SupportsPessimisticConcurrency => false;
        public ISagaIdGenerator SagaIdGenerator { get; private set; }
        public ISagaPersister SagaStorage  { get; private set; }
        public ISynchronizedStorage SynchronizedStorage { get; private set; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter  { get; private set; }
        public ISubscriptionStorage SubscriptionStorage { get; private set; }
        public IPersistTimeouts TimeoutStorage  { get; private set; }
        public IQueryTimeouts TimeoutQuery { get; private set; }
        public IOutboxStorage OutboxStorage  { get; private set; }

        public Task Configure()
        {
            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new InMemorySagaPersister();
            SynchronizedStorage = new InMemorySynchronizedStorage();
            SynchronizedStorageAdapter = new InMemoryTransactionalSynchronizedStorageAdapter();
            SubscriptionStorage = new InMemorySubscriptionStorage();
            TimeoutStorage = new InMemoryTimeoutPersister(() => DateTime.UtcNow); // todo: verify
            TimeoutQuery = new InMemoryTimeoutPersister(() => DateTime.Now);
            OutboxStorage = new InMemoryOutboxStorage();
            return TaskEx.CompletedTask;
        }

        public Task Cleanup()
        {
            return TaskEx.CompletedTask;
        }
    }
}