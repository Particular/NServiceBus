namespace NServiceBus.Distributor
{
    class EndpointRunner : IWantToRunAtStartup
    {
        public Unicast.Distributor.Distributor Distributor { get; set; }

        public void Run()
        {
            Distributor.MessageBusTransport = Service.DataTransport;
            Distributor.MessageSender = Service.MessageSender;
            Distributor.Start();
        }

        public void Stop()
        {
            Distributor.Stop();
        }
    }
}
