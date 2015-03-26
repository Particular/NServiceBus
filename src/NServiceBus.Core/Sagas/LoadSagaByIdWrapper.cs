namespace NServiceBus.Saga
{
    using System;

 
    //this class in only here until we can move to a better saga persister api
    class LoadSagaByIdWrapper<T>:SagaLoader where T : IContainSagaData
    {
        public IContainSagaData Load(ISagaPersister persister, string sagaId)
        {
            return persister.Get<T>(Guid.Parse(sagaId));
        }
    }

    interface SagaLoader
    {
        IContainSagaData Load(ISagaPersister persister, string sagaId);
    }
}