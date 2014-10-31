namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    class SagaWithUniqueProperty : Saga<SagaWithUniquePropertyData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyData> mapper)
        {
            
        }
    }
    public class SagaWithUniquePropertyData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        [Unique]
        public string UniqueString { get; set; }
    }
}