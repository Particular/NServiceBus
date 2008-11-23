
namespace NServiceBus.Saga
{
    public class SagaEntityFinder : IFindSagas<ISagaEntity>.Using<ISagaMessage>
    {
        private ISagaPersister persister;
        public virtual ISagaPersister Persister
        {
            set { this.persister = value; }
        }

        public ISagaEntity FindBy(ISagaMessage message)
        {
            if (persister != null)
                return persister.Get(message.SagaId);

            return null;
        }
    }
}
