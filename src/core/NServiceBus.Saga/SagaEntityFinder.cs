using ObjectBuilder;

namespace NServiceBus.Saga
{
    public class SagaEntityFinder : IFindSagas<ISagaEntity>.Using<ISagaMessage>
    {
        private readonly IBuilder builder;

        public SagaEntityFinder(IBuilder builder)
        {
            this.builder = builder;
        }

        public ISagaEntity FindBy(ISagaMessage message)
        {
            using (ISagaPersister persister = this.builder.Build<ISagaPersister>())
                return persister.Get(message.SagaId);
        }
    }
}
