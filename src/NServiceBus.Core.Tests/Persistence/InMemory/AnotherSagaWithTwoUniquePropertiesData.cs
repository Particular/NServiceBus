namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class AnotherSagaWithTwoUniqueProperties : Saga<AnotherSagaWithTwoUniquePropertiesData>, IHandleMessages<M1>
    {
        public Task Handle(M1 message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithTwoUniquePropertiesData> mapper)
        {
            mapper.ConfigureMapping<M1>(m => m.UniqueInt).ToSaga(s => s.UniqueInt);
            mapper.ConfigureMapping<M1>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }
    }

    class M1
    {
        public string UniqueString { get; set; }

        public int UniqueInt { get; set; }
    }

    public class AnotherSagaWithTwoUniquePropertiesData : IContainSagaData
    {
        public string UniqueString { get; set; }

        public int UniqueInt { get; set; }
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }
    }
}