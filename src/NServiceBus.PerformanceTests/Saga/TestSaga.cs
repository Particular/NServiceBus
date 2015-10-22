namespace Runner.Saga
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Sagas;

    class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            var data = context.GetSagaData<SagaData>();
            data.Number = message.Id;
            data.NumCalls++;
            return Task.FromResult(0);
        }

        public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
        {
            context.MarkAsComplete();

            return Task.FromResult(0);
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.Number);
            mapper.ConfigureMapping<CompleteSagaMessage>(m => m.Id).ToSaga(s => s.Number);
        }
    }
}