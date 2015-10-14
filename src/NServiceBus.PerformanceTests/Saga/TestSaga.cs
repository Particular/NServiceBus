namespace Runner.Saga
{
    using System.Threading.Tasks;
    using NServiceBus;

    class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            Data.Number = message.Id;
            Data.NumCalls++;
            return Task.FromResult(0);
        }

        public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
        {
            MarkAsComplete();

            return Task.FromResult(0);
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.Number);
            mapper.ConfigureMapping<CompleteSagaMessage>(m => m.Id).ToSaga(s => s.Number);
        }
    }
}