namespace NServiceBus.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    interface SagaLoader
    {
        Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, ContextBag context);
    }
}