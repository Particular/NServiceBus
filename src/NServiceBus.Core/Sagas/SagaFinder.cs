namespace NServiceBus
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Saga;

    abstract class SagaFinder
    {
        internal abstract IContainSagaData Find(IBuilder builder,SagaFinderDefinition finderDefinition, object message);
    }
}
