namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class SagaWithUniqueProperty : Saga<SagaWithUniquePropertyData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

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
        public string UniqueString { get; set; }
        public Guid Id { get; set; }

        public string Originator { get; set; }

        public string OriginalMessageId { get; set; }
    }
}