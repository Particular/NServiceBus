namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class SagaWithUniqueProperty : Saga<SagaWithUniquePropertyData>, IAmStartedByMessages<SagaUniquePropertyStartingMessage>
    {
        public Task Handle(SagaUniquePropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyData> mapper)
        {
            mapper.ConfigureMapping<SagaUniquePropertyStartingMessage>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }
    }
    public class SagaWithUniquePropertyData : ContainSagaData
    {
        public string UniqueString { get; set; }
    }

    class SagaUniquePropertyStartingMessage
    {
        public string UniqueString { get; set; }
    }
}