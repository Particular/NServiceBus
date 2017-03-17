namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Gateway.Deduplication;
    using NUnit.Framework;
    using Outbox;
    using Sagas;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration : IPersistenceTestsConfiguration
    {
        public Func<ContextBag> GetContextBagForTimeoutPersister { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSagaStorage { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForOutbox { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSubscriptions { get; set; } = () => new ContextBag();
    }

    public interface IPersistenceTestsConfiguration
    {
        bool SupportsDtc { get; }

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
    }

    public static class RequiresExtensionsForPersistenceTestsConfiguration
    {
        public static void RequiresDtcSupport(this IPersistenceTestsConfiguration configuration)
        {
            if (!configuration.SupportsDtc)
            {
                Assert.Ignore("Ignoring this test because it requires DTC transaction support from persister.");
            }
        }
    }
}