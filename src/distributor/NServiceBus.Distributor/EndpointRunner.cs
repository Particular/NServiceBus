using NServiceBus.Host;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Distributor
{
    class EndpointRunner : IMessageEndpoint
    {
        public Unicast.Distributor.Distributor Distributor { get; set; }

        public void OnStart()
        {
            Distributor.MessageBusTransport = Service.DataTransport;
            Distributor.Start();
        }

        public void OnStop()
        {
            Distributor.Stop();
        }
    }
}
