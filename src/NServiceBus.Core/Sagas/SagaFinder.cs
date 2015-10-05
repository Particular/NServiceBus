namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using ObjectBuilder;
    using Sagas;

    abstract class SagaFinder
    {
        internal abstract Task<IContainSagaData> Find(IBuilder builder, SagaFinderDefinition finderDefinition, ReadOnlyContextBag context, object message);
    }
}
