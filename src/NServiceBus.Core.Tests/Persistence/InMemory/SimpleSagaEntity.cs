namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class SimpleSagaEntitySaga:Saga<SimpleSagaEntity>,IAmStartedByMessages<StartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SimpleSagaEntity> mapper)
        {
            
        }

        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
    public class SimpleSagaEntity : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string OrderSource { get; set; }
        public DateTime OrderExpirationDate { get; set; }
        public decimal OrderCost { get; set; }
    }
}