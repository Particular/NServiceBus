namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    //this class in only here until we can move to a better saga persister api
    class LoadSagaByIdWrapper<T> : SagaLoader where T : IContainSagaData
    {
        public async Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, SynchronizedStorageSession storageSession, ContextBag context)
        {
            return await persister.Get<T>(Guid.Parse(sagaId), storageSession, context).ConfigureAwait(false);
        }
    }
}