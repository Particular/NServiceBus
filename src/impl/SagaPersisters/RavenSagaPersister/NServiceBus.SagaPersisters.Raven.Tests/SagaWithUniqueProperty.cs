using System;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class SagaWithUniqueProperty : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
    }
}