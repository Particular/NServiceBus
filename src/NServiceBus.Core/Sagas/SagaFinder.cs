namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;

    abstract class SagaFinder
    {
        public abstract Task<IContainSagaData> Find(IChildBuilder builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message);
    }
}