

namespace MyServer.Common
{
    using System;
    using NServiceBus;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Transport;

    internal class TransportReceiverFailedMessageMonitor : IWantToRunWhenBusStartsAndStops
    {                    
        public UnicastBus UnicastBus { get; set; }

        public void Start()
        {
            UnicastBus.Transport.FailedMessageProcessing += OnFailedMessageProcessing;            
        }

        void OnFailedMessageProcessing(object sender, FailedMessageProcessingEventArgs e)
        {
            Console.WriteLine("This is a first level retry attempt");            
        }

        public void Stop()
        {
        }
    }
}
