namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    class SagaWithUniqueProperty : Saga<SagaWithUniquePropertyData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyData> mapper)
        {
            mapper.ConfigureMapping<M12>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }
    }

    class M12
    {
        public string UniqueString { get; set; }
    }

    public class SagaWithUniquePropertyData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        public string UniqueString { get; set; }
    }
}