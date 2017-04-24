namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;

    public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
        {
            mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
        }
    }

    public class StartMessage
    {
        public string SomeId { get; set; }
    }

    public class TestSagaData : ContainSagaData
    {
        public string SomeId { get; set; } = "Test";

        public DateTime DateTimeProperty { get; set; }
    }
}