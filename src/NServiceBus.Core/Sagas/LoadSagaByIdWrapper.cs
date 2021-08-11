namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    //this class in only here until we can move to a better saga persister api
    class LoadSagaByIdWrapper<T> : ISagaLoader
        where T : class, IContainSagaData
    {
        public async Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, ISynchronizedStorageSession storageSession, ContextBag context, CancellationToken cancellationToken = default)
        {
            return await persister.Get<T>(Guid.Parse(sagaId), storageSession, context, cancellationToken).ConfigureAwait(false);
        }
    }
}