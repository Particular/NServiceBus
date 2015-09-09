namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Sagas;

    abstract class SagaFinder
    {
        internal abstract Task<IContainSagaData> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SagaPersistenceOptions options, object message);
    }
}
