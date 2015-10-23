namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class SagaWithTwoUniqueProperties:Saga<SagaWithTwoUniquePropertiesData>,IAmStartedByMessages<StartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithTwoUniquePropertiesData> mapper)
        {
            mapper.ConfigureMapping<M11>(m => m.UniqueInt).ToSaga(s => s.UniqueInt);
            mapper.ConfigureMapping<M11>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
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