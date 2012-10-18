using System;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    [Serializable]
    public class SagaWithUniqueProperty : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
    }

    [Serializable]
    public class SagaWithTwoUniqueProperties : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
        [Unique]
        public virtual int UniqueInt { get; set; }
    }

    [Serializable]
    public class AnotherSagaWithTwoUniqueProperties : ISagaEntity
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
        [Unique]
        public virtual int UniqueInt { get; set; }
    }
}