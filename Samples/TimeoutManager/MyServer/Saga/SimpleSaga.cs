namespace MyServer.Saga
{
    using System;
    using NServiceBus.Saga;

    public class SimpleSaga:Saga<SimpleSagaData>,IAmStartedByMessages<StartSagaMessage>
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
            MarkAsComplete();
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(s => s.OrderId, m => m.OrderId);
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToLongTimeString(),message));
        }
    }
}