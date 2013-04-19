namespace MyServer.Saga
{
    using System;
    using NServiceBus.Saga;

    public class SimpleSaga: Saga<SimpleSagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleTimeouts<MyTimeOutState>
    {
        public void Handle(StartSagaMessage message)
        {
            Data.OrderId = message.OrderId;
            var someState = new Random().Next(10);

            LogMessage("Requesting a custom timeout, state: " + someState);
            RequestTimeout(TimeSpan.FromSeconds(10), new MyTimeOutState
            {
                SomeValue = someState
            });
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
        }

        void LogMessage(string message)
        {
            Console.WriteLine("{0} - {1}", DateTime.Now.ToLongTimeString(),message);
        }

        public void Timeout(MyTimeOutState state)
        {
            LogMessage("Timeout fired, with state: " + state.SomeValue);

            LogMessage("Marking the saga as complete, be aware that this will remove the saga from the storage");
            MarkAsComplete();
        }
    }
}