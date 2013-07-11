namespace NServiceBus.Sagas.Finders
{
    using System;
    using NServiceBus.Saga;

    /// <summary>
    /// Finds sagas based on the sagaid header
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HeaderSagaIdFinder<T> : IFindSagas<T>.Using<object> where T : IContainSagaData
    {

        /// <summary>
        /// Injected persister
        /// </summary>
        public ISagaPersister SagaPersister { get; set; }

        /// <summary>
        /// Returns the saga 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
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