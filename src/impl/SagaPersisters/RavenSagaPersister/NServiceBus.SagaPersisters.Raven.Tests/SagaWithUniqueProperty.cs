using System;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using Saga;

    public class SagaWithUniqueProperty : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }

        public string NonUniqueString { get; set; }
    }
}