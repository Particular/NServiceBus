using System;
using Messages;
using NServiceBus;
using NServiceBus.Host;

namespace Server
{
    public class ServerEndpoint : IMessageEndpoint, IMessageEndpointConfiguration
    {
        public IBus Bus { get; set; }

        private delegate void InputHandlerInvoker();

        private readonly InputHandlerInvoker inputHandler;

        public ServerEndpoint()
        {
            inputHandler = HandleUserInput;
        }

        private void HandleUserInput()
        {
            bool publishIEvent = true;
            while (Console.ReadLine() != null)
            {
                IEvent eventMessage;
                if (publishIEvent)
                    eventMessage = Bus.CreateInstance<IEvent>();
                else
                    eventMessage = new EventMessage();

                eventMessage.EventId = Guid.NewGuid();
                eventMessage.Time = DateTime.Now;
                eventMessage.Duration = TimeSpan.FromSeconds(99999D);

                Bus.Publish(eventMessage);

                Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);

                publishIEvent = !publishIEvent;
            }

        }

        public void OnStart()
        {
            Console.WriteLine("This will publish IEvent and EventMessage alternately.");
            Console.WriteLine("Press 'Enter' to publish a message.To exit, Ctrl + C");


            inputHandler.BeginInvoke(null, null);
        }

        public void OnStop()
        {

        }

        public Configure ConfigureBus()
        {
            return Configure.With()
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