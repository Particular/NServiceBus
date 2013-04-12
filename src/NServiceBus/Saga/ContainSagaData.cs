namespace NServiceBus.Saga
{
    using System;

    public abstract class ContainSagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }
    }
}