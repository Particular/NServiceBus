namespace MyServer.Common
{
    using System;
    using NServiceBus;
    using NServiceBus.Unicast.Transport;

    internal class TransportReceiverFailedMessageMonitor : IWantToRunWhenBusStartsAndStops

    {                    
        public TransportReceiver TransactionalTransport { get; set; }

        public void Start()
        {
            TransactionalTransport.FailedMessageProcessing += OnFailedMessageProcessing;            
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
