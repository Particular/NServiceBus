using System;
using NServiceBus;
using NServiceBus.Host;

namespace Subscriber1
{
    public class SubscriberEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public void OnStart()
        {
            Console.WriteLine("Listening for events, press Ctrl + C to exit");
        }

        public void OnStop()
        {
            
        }

        public Configure ConfigureBus()
        {
            return Configure.With()
               .SpringBuilder()
               .XmlSerializer()
               .MsmqTransport()
                   .IsTransactional(false)
                   .PurgeOnStartup(false)
               .UnicastBus()
                   .ImpersonateSender(false)
                   .LoadMessageHandlers();
        }
    }
}