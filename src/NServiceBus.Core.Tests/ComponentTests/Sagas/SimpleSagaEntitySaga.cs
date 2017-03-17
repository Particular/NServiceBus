namespace NServiceBus.Persistence.ComponentTests
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
            mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.OrderSource);
        }
    }

    public class SimpleSagaEntity : ContainSagaData
    {
        public string OrderSource { get; set; }
    }
}