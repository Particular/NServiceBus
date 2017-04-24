// ReSharper disable UnusedMemberInSuper.Global
namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Gateway.Deduplication;
    using Outbox;
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

        ISagaIdGenerator SagaIdGenerator { get; }

        ISagaPersister SagaStorage { get; }

        ISynchronizedStorage SynchronizedStorage { get; }

        ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }

        ISubscriptionStorage SubscriptionStorage { get; }

        IPersistTimeouts TimeoutStorage { get; }

        IQueryTimeouts TimeoutQuery { get; }

        IOutboxStorage OutboxStorage { get; }

        IDeduplicateMessages GatewayStorage { get; }

        Task Configure();

        Task Cleanup();

        Task CleanupMessagesOlderThan(DateTimeOffset beforeStore);
    }
}