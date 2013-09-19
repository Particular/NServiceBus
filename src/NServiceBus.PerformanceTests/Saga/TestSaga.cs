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
        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(m => m.Id).ToSaga(s => s.Number);
            ConfigureMapping<CompleteSagaMessage>(m => m.Id).ToSaga(s => s.Number);
        }

    }
}