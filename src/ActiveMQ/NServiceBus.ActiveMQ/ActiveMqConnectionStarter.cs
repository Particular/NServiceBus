namespace NServiceBus.ActiveMQ
{
    using Apache.NMS;

    public class ActiveMqConnectionStarter : IWantToRunWhenBusStartsAndStops
    {

        public INetTxConnection Connection { get; set; }

        public void Start()
        {
            if (Connection == null)
                return;

            Connection.Start();
        }

        public void Stop()
        {
            if (Connection == null)
                return;

            Connection.Stop();
            Connection.Dispose();
        }
    }
}