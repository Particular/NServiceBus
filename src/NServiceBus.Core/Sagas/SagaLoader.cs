namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    interface ISagaLoader
    {
        Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, ISynchronizedStorageSession storageSession, ContextBag context, CancellationToken cancellationToken = default);
    }
}