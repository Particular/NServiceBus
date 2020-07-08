#pragma warning disable 1591
namespace NServiceBus.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using Outbox;
    using Persistence;
    using Sagas;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public interface IPersistenceTestsConfiguration
    {
        bool SupportsDtc { get; }

        bool SupportsOutbox { get; }

        bool SupportsFinders { get; }

        bool SupportsSubscriptions { get; }

        bool SupportsTimeouts { get; }

        bool SupportsOptimisticConcurrency { get; }
        
        bool SupportsPessimisticConcurrency { get; }

        ISagaIdGenerator SagaIdGenerator { get; }

        ISagaPersister SagaStorage { get; }

        ISynchronizedStorage SynchronizedStorage { get; }

        ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }

        ISubscriptionStorage SubscriptionStorage { get; }

        IPersistTimeouts TimeoutStorage { get; }

        IQueryTimeouts TimeoutQuery { get; }

        IOutboxStorage OutboxStorage { get; }

        Task Configure();

        Task Cleanup();

        Task CleanupMessagesOlderThan(DateTimeOffset beforeStore);
    }
}