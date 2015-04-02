namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    class SagaWithTwoUniqueProperties:Saga<SagaWithTwoUniquePropertiesData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithTwoUniquePropertiesData> mapper)
        {
            mapper.ConfigureMapping<M11>(m => m.UniqueInt).ToSaga(s => s.UniqueInt);
            mapper.ConfigureMapping<M11>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }
    }

    class M11
    {
        public string UniqueString { get; set; }
        public int UniqueInt { get; set; }
    }

    public class SagaWithTwoUniquePropertiesData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        public string UniqueString { get; set; }

        public int UniqueInt { get; set; }
    }
}