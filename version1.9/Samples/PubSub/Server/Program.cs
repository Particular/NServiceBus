using System;
using Common.Logging;
using NServiceBus;
using Messages;

namespace Server
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");

            var bus = NServiceBus.Configure.With()
                .SpringBuilder()
                .MsmqSubscriptionStorage()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                .CreateBus()
                .Start();

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
