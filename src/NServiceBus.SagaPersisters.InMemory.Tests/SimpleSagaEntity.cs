namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    public class SimpleSageEntity : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string OrderSource { get; set; }
        public DateTime OrderExpirationDate { get; set; }
        public decimal OrderCost { get; set; }
    }
}