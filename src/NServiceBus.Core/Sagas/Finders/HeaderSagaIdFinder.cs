namespace NServiceBus.Sagas.Finders
{
    using System;
    using Saga;

    /// <summary>
    /// Finds sagas based on the sagaid header
    /// </summary>
    public class HeaderSagaIdFinder<T> : IFindSagas<T>.Using<object> where T : IContainSagaData
    {

        /// <summary>
        /// Injected persister
        /// </summary>
        public ISagaPersister SagaPersister { get; set; }

        /// <summary>
        /// Returns the saga 
        /// </summary>
        public T FindBy(object message)
        {
            if (SagaPersister == null)
                return default(T);

            var sagaIdHeader = Headers.GetMessageHeader(message, Headers.SagaId);

            if (string.IsNullOrEmpty(sagaIdHeader))
                return default(T);

            return SagaPersister.Get<T>( Guid.Parse(sagaIdHeader));
        }
    }
}