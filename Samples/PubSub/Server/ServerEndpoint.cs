using System;
using Messages;
using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class ServerEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public IStartableBus Starter { get; set; }

        public void OnStart()
        {
            var bus = Starter.Start();

            Console.WriteLine("This will publish IEvent and EventMessage alternately.");
            Console.WriteLine("Press 'Enter' to publish a message.To exit, Ctrl + C");

            Action handleInput = () =>
                                     {
            bool publishIEvent = true;
            while (Console.ReadLine() != null)
            {
                IEvent eventMessage;
                if (publishIEvent)
                    eventMessage = bus.CreateInstance<IEvent>();
                else
                    eventMessage = new EventMessage();

                eventMessage.EventId = Guid.NewGuid();
                eventMessage.Time = DateTime.Now;
                eventMessage.Duration = TimeSpan.FromSeconds(99999D);

                bus.Publish(eventMessage);

                Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);

                publishIEvent = !publishIEvent;
            }
                                     };

            handleInput.BeginInvoke(null, null);
        }

        public void OnStop()
        {

        }

        public Configure Configure()
        {
            return NServiceBus.Configure.With()
                .SpringBuilder()
                //.DbSubscriptionStorage()
                //        .Table("Subscriptions")
                //        .SubscriberEndpointColumnName("SubscriberEndpoint")
                //        .MessageTypeColumnName("MessageType")
                .MsmqSubscriptionStorage()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false);
        }
    }
}