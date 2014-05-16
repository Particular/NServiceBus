namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    public class SagaWithUniqueProperty : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        [Unique]
        public string UniqueString { get; set; }
    }
}