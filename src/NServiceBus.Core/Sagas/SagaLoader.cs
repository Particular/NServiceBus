namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    interface SagaLoader
    {
        Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, SynchronizedStorageSession storageSession, ContextBag context);
    }
}