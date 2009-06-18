using System;
using Messages;

namespace NServiceBus.Host.ExampleEndpoint
{
    [EndpointName("Example")]
    public class BasicEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public IBus Bus{ get; set;}


        public Configure ConfigureBus(Configure config)
        {
            return config.StructureMapBuilder()
                            .XmlSerializer()
                            .MsmqTransport()
                            .IsTransactional(false)
                            .PurgeOnStartup(false)
                            .UnicastBus()
                            .ImpersonateSender(false)
                            .LoadMessageHandlers();
        }
        public void OnStart()
        {
        }

        public void OnStop()
        {
        }

    }

    public class EventMessageHandler : IMessageHandler<EventMessage>
    {
        public void Handle(EventMessage message)
        {
            Console.WriteLine("Subscriber 1 received EventMessage with Id {0}.", message.EventId);
            Console.WriteLine("Message time: {0}.", message.Time);
            Console.WriteLine("Message duration: {0}.", message.Duration);
        }
    }
}
