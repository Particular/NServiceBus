namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;

    abstract class SagaFinder
    {
        public abstract Task<IContainSagaData> Find(IBuilder builder, SagaFinderDefinition finderDefinition, ISynchronizedStorageSession storageSession, ContextBag context, object message);
    }
}