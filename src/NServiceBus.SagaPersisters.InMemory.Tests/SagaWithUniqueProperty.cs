using System;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using Saga;

    public class SagaWithUniqueProperty : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
    }

    public class SagaWithTwoUniqueProperties : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        [Unique]
        public virtual string UniqueString { get; set; }
        [Unique]
        public virtual int UniqueInt { get; set; }
    }

    public class AnotherSagaWithTwoUniqueProperties : IContainSagaData
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