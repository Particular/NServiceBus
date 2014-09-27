namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    class AnotherSagaWithTwoUniqueProperties:Saga<AnotherSagaWithTwoUniquePropertiesData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithTwoUniquePropertiesData> mapper)
        {
            
        }
    }

    public class AnotherSagaWithTwoUniquePropertiesData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        [Unique]
        public string UniqueString { get; set; }
        [Unique]
        public int UniqueInt { get; set; }
    }
}