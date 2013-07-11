using Messages;
using System;
using NServiceBus.Saga;

namespace Server.Saga
{
    public class SimpleSaga : Saga<SimpleSagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleTimeouts<MyTimeOutState>
    {
        public void Handle(StartSagaMessage message)
        {
            Data.OrderId = message.OrderId;
            var someState = new Random().Next(10);

            RequestTimeout(TimeSpan.FromSeconds(10), someState);
            LogMessage("v2.6 Timeout (10s) requested with state: " + someState);
        }

        [Obsolete]
        public override void Timeout(object state)
        {
            LogMessage("v2.6 Timeout fired, with state: " + state);

            var someState = new Random().Next(10);

            LogMessage("Requesting a custom timeout v3.0 style, state: " + someState);
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
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(),message));
        }

        public void Timeout(MyTimeOutState state)
        {
            LogMessage("v3.0 Timeout fired, with state: " + state.SomeValue);

            LogMessage("Marking the saga as complete, be aware that this will remove the document from the storage (RavenDB)");
            MarkAsComplete();
        }
    }
}