#pragma warning disable 1591
namespace NServiceBus.PersistenceTests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using Outbox;
    using Persistence;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NServiceBus;

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

    public partial class PersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration(TimeSpan? fromMilliseconds = null)
        {
            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new InMemorySagaPersister();
            SynchronizedStorage = new InMemorySynchronizedStorage();
            SynchronizedStorageAdapter = new InMemoryTransactionalSynchronizedStorageAdapter();
            SubscriptionStorage = new InMemorySubscriptionStorage();
            TimeoutStorage = new InMemoryTimeoutPersister(() => DateTime.UtcNow); // todo: verify
            TimeoutQuery = new InMemoryTimeoutPersister(() => DateTime.Now);
            OutboxStorage = new InMemoryOutboxStorage();
        }

        public bool SupportsDtc => false; // TODO: verify if this is true
        public bool SupportsOutbox => true;

        public bool SupportsFinders => true;  // TODO: verify if we actually need this as we think it should only be invoked by core
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