namespace NServiceBus.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    //this class in only here until we can move to a better saga persister api
    class LoadSagaByIdWrapper<T> : SagaLoader where T : IContainSagaData
    {
        public async Task<IContainSagaData> Load(ISagaPersister persister, string sagaId, ContextBag context)
        {
            return await persister.Get<T>(Guid.Parse(sagaId), context).ConfigureAwait(false);
        }
    }
}