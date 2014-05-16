namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    public class AnotherSimpleSageEntity : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string ProductSource { get; set; }
        public DateTime ProductExpirationDate { get; set; }
        public decimal ProductCost { get; set; }
    }
}