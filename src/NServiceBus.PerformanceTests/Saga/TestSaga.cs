namespace Runner.Saga
{
    using NServiceBus;
    using NServiceBus.Saga;

    class TestSaga : Saga<SagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>
    {
        public void Handle(StartSagaMessage message)
        {
            Data.Number = message.Id;
            Data.NumCalls++;
        }

        public void Handle(CompleteSagaMessage message)
        {
            MarkAsComplete();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.Number);
            mapper.ConfigureMapping<CompleteSagaMessage>(m => m.Id).ToSaga(s => s.Number);
        }
    }
}