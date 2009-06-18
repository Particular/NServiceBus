using System;
using Messages;
using NServiceBus;
using NServiceBus.Host;

namespace Subscriber2
{
    public class MessageEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public IBus Bus { get; set; }
        public void OnStart()
        {
            Bus.Subscribe<IEvent>();

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
                .IsTransactional(false)
                .PurgeOnStartup(false)
                .UnicastBus()
                .ImpersonateSender(false)
                .DoNotAutoSubscribe()
                .LoadMessageHandlers();
        }
    }

}