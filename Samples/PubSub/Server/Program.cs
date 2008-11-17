using System;
using Common.Logging;
using NServiceBus;
using Messages;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.DB.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using NServiceBus.Unicast.Subscriptions.Msmq.Config;
using ObjectBuilder;

namespace Server
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            new ConfigMsmqSubscriptionStorage(builder);
            //new ConfigDbSubscriptionStorage(builder)
            //    .Table("Subscriptions")
            //    .SubscriberEndpointParameterName("SubscriberEndpoint")
            //    .MessageTypeParameterName("MessageType");

            builder.ConfigureComponent<MessageMapper>(ComponentCallModelEnum.Singleton);

            NServiceBus.Serializers.Configure.InterfaceToXMLSerializer.With(builder);
            //NServiceBus.Serializers.Configure.XmlSerializer.With(builder);

            new ConfigMsmqTransport(builder)
                .IsTransactional(true)
                .PurgeOnStartup(false);

            new ConfigUnicastBus(builder)
                .ImpersonateSender(false);

            IBus bus = builder.Build<IBus>();
            bus.Start();

            Console.WriteLine("This will publish IEvent and EventMessage alternately.");
            Console.WriteLine("Press 'Enter' to publish a message. Enter a number to publish that number of events. To exit, press 'q' and then 'Enter'.");

            bool publishIEvent = true;
            string read;
            while ((read = Console.ReadLine().ToLower()) != "q")
            {
                int number;
                if (!int.TryParse(read, out number))
                    number = 1;

                for (int i = 0; i < number; i++)
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
            }
        }
    }
}
