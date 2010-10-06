using NServiceBus.Unicast.Queuing.Msmq;

namespace NServiceBus.Distributor
{
    class EndpointRunner : IWantToRunAtStartup
    {
        public Unicast.Distributor.Distributor Distributor { get; set; }

        public void Run()
        {
            Distributor.MessageBusTransport = Service.DataTransport;
            Distributor.MessageSender = new MsmqMessageSender();
            Distributor.Start();
        }

        public void Stop()
        {
            Distributor.Stop();
        }
    }
}
