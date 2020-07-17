namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public interface IPersistenceTestsConfiguration
    {
        bool SupportsDtc { get; }

        bool SupportsOutbox { get; }

        bool SupportsFinders { get; } // TODO: why do we require this?

        bool SupportsSubscriptions { get; }

        bool SupportsTimeouts { get; }

        bool SupportsOptimisticConcurrency { get; }

        bool SupportsPessimisticConcurrency { get; }

        ISagaIdGenerator SagaIdGenerator { get; }

        ISagaPersister SagaStorage { get; }

        ISynchronizedStorage SynchronizedStorage { get; }

        TimeSpan? SessionTimeout { get; }

        ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }

        ISubscriptionStorage SubscriptionStorage { get; }

        IPersistTimeouts TimeoutStorage { get; }

        IQueryTimeouts TimeoutQuery { get; }

        IOutboxStorage OutboxStorage { get; }

        SagaMetadataCollection SagaMetadataCollection { get; }

        Task Configure();

        Task Cleanup();

        Func<ContextBag> GetContextBagForTimeoutPersister { get; }
        Func<ContextBag> GetContextBagForSagaStorage { get; } //TODO why is this not used?
        Func<ContextBag> GetContextBagForOutbox { get; }
        Func<ContextBag> GetContextBagForSubscriptions { get; }
    }

    // Consumers of this source package have to implement the remaining properties via partial class to configure the tests infrastructure.
    public partial class PersistenceTestsConfiguration : IPersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration(TimeSpan? sessionTimeout = null)
        {
            SessionTimeout = sessionTimeout;
        }

        public Func<ContextBag> GetContextBagForTimeoutPersister { get; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSagaStorage { get; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForOutbox { get; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSubscriptions { get; } = () => new ContextBag();
        public TimeSpan? SessionTimeout { get; }

        public SagaMetadataCollection SagaMetadataCollection
        {
            get
            {
                if (sagaMetadataCollection == null)
                {
                    var sagaTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(Saga).IsAssignableFrom(t) || typeof(IFindSagas<>).IsAssignableFrom(t) || typeof(IFinder).IsAssignableFrom(t)).ToArray();
                    sagaMetadataCollection = new SagaMetadataCollection();
                    sagaMetadataCollection.Initialize(sagaTypes);
                }

                return sagaMetadataCollection;
            }
            set { sagaMetadataCollection = value; }
        }

        SagaMetadataCollection sagaMetadataCollection;
    }
}