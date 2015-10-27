namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class SimpleSagaEntitySaga : Saga<SimpleSagaEntity>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SimpleSagaEntity> mapper)
        {
        }
    }

    public class SimpleSagaEntity : ContainSagaData
    {
        public string OrderSource { get; set; }
    }
}