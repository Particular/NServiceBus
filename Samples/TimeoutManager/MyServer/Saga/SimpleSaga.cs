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

            RequestUtcTimeout(TimeSpan.FromSeconds(10), someState);
            LogMessage("v2.6 Timeout (10s) requested with state: " + someState);
        }

        public override void Timeout(object state)
        {
            LogMessage("v2.6 Timeout fired, with state: " + state);

            var someState = new Random().Next(10);

            LogMessage("Requesting a custom timeout v3.0 style, state: " + someState);
            RequestUtcTimeout<MyTimeOutState>(TimeSpan.FromSeconds(10),t=>t.SomeValue = someState);
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(s => s.OrderId, m => m.OrderId);
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("{0} - {1} - SagaId:{2}", DateTime.Now.ToLongTimeString(),message,Data.Id));
        }

        public void Timeout(MyTimeOutState state)
        {
            LogMessage("v3.0 Timeout fired, with state: " + state.SomeValue);

            LogMessage("Marking the saga as complete, be aware that this will remove the document from the storage (RavenDB)");
            MarkAsComplete();
        }
    }
}