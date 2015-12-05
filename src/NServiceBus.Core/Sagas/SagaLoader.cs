namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;

    interface SagaLoader
    {
        Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, SynchronizedStorageSession storageSession, ContextBag context);
    }
}