namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;

    class AnotherSagaTwoUniqueProperty : Saga<AnotherSagaWithUniquePropertyData>, IAmStartedByMessages<TwoUniquePropertyStartingMessage>
    {
        public Task Handle(TwoUniquePropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithUniquePropertyData> mapper)
        {
            mapper.ConfigureMapping<TwoUniquePropertyStartingMessage>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }
    }

    class TwoUniquePropertyStartingMessage
    {
        public string UniqueString { get; set; }
    }

    public class AnotherSagaWithUniquePropertyData : ContainSagaData
    {
        public string UniqueString { get; set; }
    }
}