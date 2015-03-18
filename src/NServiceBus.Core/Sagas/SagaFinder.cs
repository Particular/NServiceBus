namespace NServiceBus
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Saga;
    using NServiceBus.Sagas;

    abstract class SagaFinder
    {
        internal abstract IContainSagaData Find(IBuilder builder,SagaFinderDefinition finderDefinition, object message);
    }
}