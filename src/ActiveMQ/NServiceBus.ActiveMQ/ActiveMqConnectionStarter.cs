namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;
    using NServiceBus.Config;

    public class ActiveMqConnectionStarter : IWantToRunWhenBusStartsAndStops, IWantToRunWhenConfigurationIsComplete
    {

        public INetTxConnection Connection { get; set; }

        public void Start()
        {
            if (Connection == null)
                return;

            if (!Connection.IsStarted)
                Connection.Start();
        }

        public void Stop()
        {
            if (Connection == null)
                return;

            if (Connection.IsStarted)
                Connection.Stop();

            Connection.Dispose();
        }

        //we need to connect to the broker even if the bus never starts to support send only endpoints
        public void Run()
        {
            if (Connection == null)
                return;

            if (!Connection.IsStarted)
                Connection.Start();
        }
    }
}