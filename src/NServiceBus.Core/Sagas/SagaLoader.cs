namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

#pragma warning disable IDE1006 // Naming Styles
    interface SagaLoader
#pragma warning restore IDE1006 // Naming Styles
    {
        Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, SynchronizedStorageSession storageSession, ContextBag context, CancellationToken cancellationToken = default);
    }
}