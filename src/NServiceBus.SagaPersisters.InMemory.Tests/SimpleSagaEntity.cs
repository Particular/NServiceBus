namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    public class SimpleSageEntity : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }

        public virtual string OrderSource { get; set; }
        public virtual DateTime OrderExpirationDate { get; set; }
        public virtual decimal OrderCost { get; set; }
    }
    public class AnotherSimpleSageEntity : IContainSagaData
    {
        public virtual Guid Id { get; set; }
        public virtual string Originator { get; set; }
        public virtual string OriginalMessageId { get; set; }

        public virtual string ProductSource { get; set; }
        public virtual DateTime ProductExpirationDate { get; set; }
        public virtual decimal ProductCost { get; set; }
    }
}