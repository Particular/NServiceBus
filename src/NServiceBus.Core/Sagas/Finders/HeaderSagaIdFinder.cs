namespace NServiceBus.Sagas.Finders
{
    using System;
    using Saga;

    class HeaderSagaIdFinder<T> : IFindSagas<T>.Using<object> where T : IContainSagaData
    {
        public ISagaPersister SagaPersister { get; set; }

        public IBus Bus { get; set; }

        public T FindBy(object message)
        {
            if (SagaPersister == null)
                return default(T);

            var sagaIdHeader = Bus.GetMessageHeader(message, Headers.SagaId);

            if (string.IsNullOrEmpty(sagaIdHeader))
                return default(T);

            return SagaPersister.Get<T>( Guid.Parse(sagaIdHeader));
        }
    }
}