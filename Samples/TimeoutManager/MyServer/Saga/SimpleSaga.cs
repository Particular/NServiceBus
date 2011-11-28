namespace MyServer
{
    using System;
    using NServiceBus.Saga;

    public class SimpleSaga:Saga<SimpleSagaData>,IAmStartedByMessages<StartSagaMessage>
    {
        public void Handle(StartSagaMessage message)
        {
            RequestTimeout(TimeSpan.FromSeconds(20),null);
            Console.WriteLine(DateTime.Now.ToShortTimeString() + " - Timeout requested");
        }

        public override void Timeout(object state)
        {
            Console.WriteLine(DateTime.Now.ToShortTimeString() + " - Timeout fired");
        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(s => s.OrderId, m => m.OrderId);
        }

    }
}