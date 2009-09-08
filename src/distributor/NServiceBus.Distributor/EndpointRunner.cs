using NServiceBus.Host;

namespace NServiceBus.Distributor
{
    class EndpointRunner : IWantToRunAtStartup
    {
        public Unicast.Distributor.Distributor Distributor { get; set; }

        public void Run()
        {
            Distributor.MessageBusTransport = Service.DataTransport;
            Distributor.Start();
        }

        public void Stop()
        {
            Distributor.Stop();
        }
    }
}
