namespace MyPublisher.Saga
{
    using System;

    using MyMessages.Publisher;
    using MyMessages.Subscriber1;

    using NServiceBus;
    using NServiceBus.Saga;

    public class SimpleSaga : Saga<SimpleSagaData>,
                              IAmStartedByMessages<StartSagaMessage>,
                              IHandleMessages<CompleteSagaMessage>,
                              IHandleTimeouts<MyTimeOutState>
    {
        public void Handle(StartSagaMessage message)
        {
            this.Data.OrderId = message.OrderId;
            var someState = new Random().Next(10);

            this.RequestTimeout<MyTimeOutState>(TimeSpan.FromSeconds(5), t => t.SomeValue = someState);
            this.RequestTimeout<MyTimeOutState>(TimeSpan.FromSeconds(8), t => t.SomeValue = someState);
            this.RequestTimeout<MyTimeOutState>(TimeSpan.FromSeconds(11), t => t.SomeValue = someState);

            // this.ReplyToOriginator<StartedSaga>(m => m.OrderId = this.Data.OrderId);
        }

        public override void ConfigureHowToFindSaga()
        {
            this.ConfigureMapping<StartSagaMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
            this.ConfigureMapping<CompleteSagaMessage>(s => s.OrderId).ToSaga(m => m.OrderId);
        }

        void LogMessage(string message)
        {
            Console.WriteLine("{0} - {1} - SagaId:{2}", DateTime.Now.ToLongTimeString(), message, this.Data.Id);
        }

        public void Timeout(MyTimeOutState state)
        {
            this.LogMessage("Timeout fired fo saga {0}, with state: " + state.SomeValue);

            this.LogMessage("Marking the saga as complete, be aware that this will remove the document from the storage (RavenDB)");
            this.MarkAsComplete();
        }

        public void Handle(CompleteSagaMessage message)
        {
            this.LogMessage("Completing Saga");
            this.LogMessage("Marking the saga as complete, be aware that this will remove the document from the storage (RavenDB)");

            this.MarkAsComplete();
            this.Bus.Send<MyRequest1>(
                r =>
                    {
                        r.Time = DateTime.UtcNow;
                        r.Duration = TimeSpan.FromMinutes(10);
                        r.CommandId = Guid.NewGuid();
                    });

            if (message.ThrowDuringCompletion)
            {
                this.LogMessage("Throwing exception");
                throw new Exception();
            }
        }
    }
}