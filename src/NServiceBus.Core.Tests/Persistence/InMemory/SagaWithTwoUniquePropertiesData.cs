namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    class SagaWithTwoUniqueProperties:Saga<SagaWithTwoUniquePropertiesData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithTwoUniquePropertiesData> mapper)
        {
            
        }
    }

    public class SagaWithTwoUniquePropertiesData : IContainSagaData
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