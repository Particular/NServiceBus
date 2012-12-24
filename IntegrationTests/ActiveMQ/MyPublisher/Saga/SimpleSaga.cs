namespace MyPublisher.Saga
{
    using System;

    using MyMessages.Publisher;

    using NServiceBus.Saga;

    public class SimpleSaga : Saga<SimpleSagaData>,
                              IAmStartedByMessages<StartSagaMessage>,
                              IHandleTimeouts<MyTimeOutState>
    {
        public void Handle(StartSagaMessage message)
        {
            this.Data.OrderId = message.OrderId;
            var someState = new Random().Next(10);

            this.RequestTimeout<MyTimeOutState>(TimeSpan.FromSeconds(5), t => t.SomeValue = someState);

            // this.ReplyToOriginator<StartedSaga>(m => m.OrderId = this.Data.OrderId);
        }

        public override void ConfigureHowToFindSaga()
        {
            this.ConfigureMapping<StartSagaMessage>(s => s.OrderId, m => m.OrderId);
        }

        void LogMessage(string message)
        {
            Console.WriteLine("{0} - {1} - SagaId:{2}", DateTime.Now.ToLongTimeString(), message, this.Data.Id);
        }

        public void Timeout(MyTimeOutState state)
        {
            this.LogMessage("Timeout fired, with state: " + state.SomeValue);

            this.LogMessage("Marking the saga as complete, be aware that this will remove the document from the storage (RavenDB)");
            this.MarkAsComplete();
        }
    }
}