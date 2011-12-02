namespace NServiceBus.Sagas.Impl.Finders
{
    using System;
    using NServiceBus.Saga;

    /// <summary>
    /// Finds sagas based on the sagaid header
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HeaderSagaIdFinder<T> : IFindSagas<T>.Using<object> where T : ISagaEntity
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

            var sagaIdHeader = message.GetHeader(Headers.SagaId);

            if (string.IsNullOrEmpty(sagaIdHeader))
                return default(T);

            return SagaPersister.Get<T>( Guid.Parse(sagaIdHeader));
        }
    }
}