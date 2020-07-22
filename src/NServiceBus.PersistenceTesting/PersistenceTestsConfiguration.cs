namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => throw new NotImplementedException();
        public bool SupportsOutbox => throw new NotImplementedException();
        public bool SupportsFinders => throw new NotImplementedException();
        public bool SupportsSubscriptions => throw new NotImplementedException();
        public bool SupportsTimeouts => throw new NotImplementedException();
        public bool SupportsOptimisticConcurrency => throw new NotImplementedException();
        public bool SupportsPessimisticConcurrency => throw new NotImplementedException();
        public ISagaIdGenerator SagaIdGenerator => throw new NotImplementedException();
        public ISagaPersister SagaStorage => throw new NotImplementedException();
        public ISynchronizedStorage SynchronizedStorage => throw new NotImplementedException();
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter => throw new NotImplementedException();
        public ISubscriptionStorage SubscriptionStorage => throw new NotImplementedException();
        public IPersistTimeouts TimeoutStorage => throw new NotImplementedException();
        public IQueryTimeouts TimeoutQuery => throw new NotImplementedException();
        public IOutboxStorage OutboxStorage => throw new NotImplementedException();

        public Task Configure(object testClass) => throw new NotImplementedException();
        public Task Cleanup() => throw new NotImplementedException();
    }
}