using System;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport.Transactional;

namespace MyServer.Common
{
    using NServiceBus;

    internal class TransactionalTransportFailedMessageMonitor : IWantToRunWhenTheBusStarts

    {                    
        public TransactionalTransport TransactionalTransport { get; set; }

        public void Run()
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