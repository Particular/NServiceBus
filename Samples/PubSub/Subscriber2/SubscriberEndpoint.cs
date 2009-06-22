using System;
using Messages;
using NServiceBus;
using NServiceBus.Host;

namespace Subscriber2
{
    //NOTE: here we are using the EndpointName attribute to distinguish this endpoint from Subscriber1 when installed as a Windows service
    [EndpointName("Subscriber2")]
    public class SubscriberEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
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
                .DoNotAutoSubscribe()
                .LoadMessageHandlers();
        }
    }

}