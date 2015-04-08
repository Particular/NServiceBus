namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using Saga;

    class AnotherSagaWithTwoUniqueProperties:Saga<AnotherSagaWithTwoUniquePropertiesData>, IHandleMessages<M1>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithTwoUniquePropertiesData> mapper)
        {
            mapper.ConfigureMapping<M1>(m => m.UniqueInt).ToSaga(s => s.UniqueInt);
            mapper.ConfigureMapping<M1>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        public void Handle(M1 message)
        {
            throw new NotImplementedException();
        }
    }

    class M1
    {
        public string UniqueString { get; set; }

        public int UniqueInt { get; set; }
    }

    public class AnotherSagaWithTwoUniquePropertiesData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }

        public string UniqueString { get; set; }

        public int UniqueInt { get; set; }
    }
}