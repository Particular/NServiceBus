namespace NServiceBus.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    interface SagaLoader
    {
        Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, SynchronizedStorageSession storageSession, ContextBag context);
    }
}