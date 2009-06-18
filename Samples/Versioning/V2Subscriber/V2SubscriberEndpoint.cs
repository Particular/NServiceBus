using System;
using NServiceBus;
using NServiceBus.Host;

namespace V2Subscriber
{
    public class V2SubscriberEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public void OnStart()
        {
            Console.WriteLine("Listening for events, press Ctrl + C to exit");
        }

        public void OnStop()
        {
        }

        public Configure ConfigureBus(Configure config)
        {
            return config
                .SpringBuilder()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers();
        }
    }
}