namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    class SimpleSagaEntitySaga:Saga<SimpleSagaEntity>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SimpleSagaEntity> mapper)
        {
            
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