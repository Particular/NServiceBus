namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => throw new NotImplementedException();

        public bool SupportsOutbox => throw new NotImplementedException();

        public bool SupportsFinders => throw new NotImplementedException();

        public bool SupportsPessimisticConcurrency => throw new NotImplementedException();

        public ISagaIdGenerator SagaIdGenerator => throw new NotImplementedException();

        public ISagaPersister SagaStorage => throw new NotImplementedException();

        public ISynchronizedStorage SynchronizedStorage => throw new NotImplementedException();

        public ISynchronizedStorageAdapter SynchronizedStorageAdapter => throw new NotImplementedException();

        public IOutboxStorage OutboxStorage => throw new NotImplementedException();

        public Task Configure() => throw new NotImplementedException();

        public Task Cleanup() => throw new NotImplementedException();
    }
}