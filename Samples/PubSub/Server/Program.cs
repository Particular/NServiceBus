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
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            IBus bServer = builder.Build<IBus>();
            bServer.Start();

            Console.WriteLine("Press 'Enter' to publish a message. Enter a number to publish that number of events. To exit, press 'q' and then 'Enter'.");
            string read;
            while ((read = Console.ReadLine().ToLower()) != "q")
            {
                int number;
                if (!int.TryParse(read, out number))
                    number = 1;

                for (int i = 0; i < number; i++)
                {
                    EventMessage eventMessage = new EventMessage();
                    eventMessage.EventId = Guid.NewGuid();

                    bServer.Publish(eventMessage);

                    Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);
                }
            }
        }
    }
}
