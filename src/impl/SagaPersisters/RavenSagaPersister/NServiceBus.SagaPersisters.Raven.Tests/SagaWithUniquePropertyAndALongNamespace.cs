namespace NServiceBus.SagaPersisters.Raven.Tests.AveryLoooooooooooooooooooongNamespace
{
    using System;
    using Saga;

    public class SagaWithUniquePropertyAndALongNamespace : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }

    }
}