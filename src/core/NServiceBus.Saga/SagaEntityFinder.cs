
namespace NServiceBus.Saga
{
    /// <summary>
    /// Single-call object used to find saga entities using saga ids from a saga persister.
    /// </summary>
    public class SagaEntityFinder : IFindSagas<ISagaEntity>.Using<ISagaMessage>
    {
        /// <summary>
        /// Saga persister used to find sagas.
        /// </summary>
        public virtual ISagaPersister Persister { get; set; }

        /// <summary>
        /// Given a saga message, uses the contained saga id to query the object's saga persister.
        /// </summary>
        /// <param name="message">Message containing the saga id.</param>
        /// <returns>The saga entity if found, otherwise null.</returns>
        public ISagaEntity FindBy(ISagaMessage message)
        {
            if (Persister != null)
                return Persister.Get(message.SagaId);

            return null;
        }
    }
}
