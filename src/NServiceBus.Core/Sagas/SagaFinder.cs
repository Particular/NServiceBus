namespace NServiceBus
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Saga;
    using NServiceBus.Unicast.Messages;

    abstract class SagaFinder
    {
        internal abstract IContainSagaData Find(IBuilder builder,SagaFinderDefinition finderDefinition, LogicalMessage message);
    }
}