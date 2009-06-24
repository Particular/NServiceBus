using System;
using NServiceBus;
using NServiceBus.Host;

namespace Subscriber1
{
    public class SubscriberEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public IStartableBus Starter { get; set; }

        public void OnStart()
        {
            Starter.Start();
            Console.WriteLine("Listening for events, press Ctrl + C to exit");
        }

        public void OnStop()
        {
            
        }

        public Configure Configure()
        {
            return NServiceBus.Configure.With()
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