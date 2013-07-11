namespace MyServer.Saga
{
    using System;
    using NServiceBus.Saga;

    public class SimpleSaga:Saga<SimpleSagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleTimeouts<MyTimeOutState>
    {
        public void Handle(StartSagaMessage message)
        {
            Data.OrderId = message.OrderId;
            var someState = new Random().Next(10);

            RequestTimeout<MyTimeOutState>(TimeSpan.FromSeconds(10), t => t.SomeValue = someState);
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("{0} - {1} - SagaId:{2}", DateTime.Now.ToLongTimeString(),message,Data.Id));
        }

        public void Timeout(MyTimeOutState state)
        {
            LogMessage("Timeout fired, with state: " + state.SomeValue);

            LogMessage("Marking the saga as complete, be aware that this will remove the document from the storage (RavenDB)");
            MarkAsComplete();
        }
    }
}