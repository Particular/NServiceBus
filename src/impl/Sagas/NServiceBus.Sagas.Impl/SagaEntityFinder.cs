
using NServiceBus.Saga;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Single-call object used to find saga entities using saga ids from a saga persister.
    /// </summary>
    public class SagaEntityFinder<T> : IFindSagas<T>.Using<ISagaMessage> where T : ISagaEntity
    {
        /// <summary>
        /// Saga persister used to find sagas.
        /// </summary>
        public ISagaPersister Persister { get; set; }

        /// <summary>
        /// Finds the saga entity type T using the saga Id in the given message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public T FindBy(ISagaMessage message)
        {
            if (Persister != null)
                return Persister.Get<T>(message.SagaId);

            return default(T);
        }
    }
}
