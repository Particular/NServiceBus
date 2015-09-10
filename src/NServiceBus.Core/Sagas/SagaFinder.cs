namespace NServiceBus
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Sagas;

    abstract class SagaFinder
    {
        internal abstract IContainSagaData Find(IBuilder builder,SagaFinderDefinition finderDefinition, SagaPersistenceOptions options, object message);
    }
}
