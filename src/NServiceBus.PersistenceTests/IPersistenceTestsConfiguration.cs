namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    public interface IPersistenceTestsConfiguration
    {
        bool SupportsDtc { get; }

        bool SupportsOutbox { get; }

        bool SupportsFinders { get; }

        bool SupportsPessimisticConcurrency { get; }

        ISagaIdGenerator SagaIdGenerator { get; }

        ISagaPersister SagaStorage { get; }

        TimeSpan? SessionTimeout { get; }

        IOutboxStorage OutboxStorage { get; }

        SagaMetadataCollection SagaMetadataCollection { get; }

        Task Configure(CancellationToken cancellationToken = default);

        Task Cleanup(CancellationToken cancellationToken = default);

        Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; }

        Func<ContextBag> GetContextBagForSagaStorage { get; }

        Func<ContextBag> GetContextBagForOutbox { get; }
    }

    // Consumers of this source package have to implement the remaining properties via partial class to configure the tests infrastructure.
    public partial class PersistenceTestsConfiguration : IPersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration(TestVariant variant)
        {
            SessionTimeout = variant.SessionTimeout;
            Variant = variant;
        }

        public Func<ContextBag> GetContextBagForSagaStorage { get; private set; } = () => new ContextBag();

        public Func<ContextBag> GetContextBagForOutbox { get; private set; } = () => new ContextBag();

        public TimeSpan? SessionTimeout { get; }

        public TestVariant Variant { get; }

        public SagaMetadataCollection SagaMetadataCollection
        {
            get
            {
                if (sagaMetadataCollection == null)
                {
                    var sagaTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(Saga).IsAssignableFrom(t) || typeof(ISagaFinder<,>).IsAssignableFrom(t) || typeof(IFinder).IsAssignableFrom(t)).ToArray();
                    sagaMetadataCollection = new SagaMetadataCollection();
                    sagaMetadataCollection.Initialize(sagaTypes);
                }

                return sagaMetadataCollection;
            }
            set => sagaMetadataCollection = value;
        }

        // Used by the SagaPersisterTests TestFixtureSource attribute
        // Change this value via static constructor to create custom test permutations
        public static object[] SagaVariants { get; } = new[]
        {
            new TestFixtureData(new TestVariant("default"))
        };

        // Used by the OutboxStorageTests TestFixtureSource attribute
        // Change this value via static constructor to create custom test permutations
        public static object[] OutboxVariants { get; } = new[]
        {
            new TestFixtureData(new TestVariant("default"))
        };

        SagaMetadataCollection sagaMetadataCollection;
    }
}