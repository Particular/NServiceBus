namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using Apache.NMS;

    public class ActiveMqConnectionStarter : IWantToRunWhenBusStartsAndStops
    {
        private readonly INetTxConnection connection;

        public ActiveMqConnectionStarter(INetTxConnection connection)
        {
            this.connection = connection;
        }

        public void Start()
        {
            this.connection.Start();
        }

        public void Stop()
        {
            this.connection.Stop();
        }
    }
}