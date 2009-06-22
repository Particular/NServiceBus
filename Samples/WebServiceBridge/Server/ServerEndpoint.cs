using System;
using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class ServerEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public IBus Bus { get; set; }
        
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
                .MsmqSubscriptionStorage()
                .BinarySerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers();
        }
    }
}