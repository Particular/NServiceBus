namespace NServiceBus.Saga
{
    using System;

 
    //this class in only here until we can move to a better saga persister api
    class LoadSagaByIdWrapper<T>:SagaLoader where T : IContainSagaData
    {
        public IContainSagaData Load(ISagaPersister persister, SagaMetadata metadata, string sagaId)
        {
            return persister.Get<T>(metadata, Guid.Parse(sagaId));
        }
    }

    interface SagaLoader
    {
        IContainSagaData Load(ISagaPersister persister, SagaMetadata metadata, string sagaId);
    }
}