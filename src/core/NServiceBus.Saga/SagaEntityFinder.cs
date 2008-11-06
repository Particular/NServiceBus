using ObjectBuilder;

namespace NServiceBus.Saga
{
    public class SagaEntityFinder : IFindSagas<ISagaEntity>
    {
        private readonly IBuilder builder;

        public SagaEntityFinder(IBuilder builder)
        {
            this.builder = builder;
        }

        public ISagaEntity FindBy(IMessage message)
        {
            ISagaMessage sagaMessage = message as ISagaMessage;

            if (sagaMessage != null)
                using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
                    return persister.Get(sagaMessage.SagaId);

            return null;
        }
    }
}
