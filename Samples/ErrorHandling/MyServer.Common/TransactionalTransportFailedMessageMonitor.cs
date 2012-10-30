namespace MyServer.Common
{
    using System;
    using NServiceBus;
    using NServiceBus.Unicast.Transport.Transactional;

    internal class TransactionalTransportFailedMessageMonitor : IWantToRunWhenBusStartsAndStops

    {                    
        public TransactionalTransport TransactionalTransport { get; set; }

        public void Start()
        {
            TransactionalTransport.FailedMessageProcessing += OnFailedMessageProcessing;            
        }

        void OnFailedMessageProcessing(object sender, NServiceBus.Unicast.Transport.FailedMessageProcessingEventArgs e)
        {
            Console.WriteLine("This is a first level retry attempt");            
        }

        public void Stop()
        {
        }
    }
}
