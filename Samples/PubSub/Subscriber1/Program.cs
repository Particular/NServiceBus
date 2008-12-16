using System;
using Common.Logging;
using Messages;
using NServiceBus;

namespace Subscriber1
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");

            var bus = NServiceBus.Configure.With()
                .SpringBuilder()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(EventMessageHandler).Assembly
                    )
                .CreateBus()
                .Start();

            Console.WriteLine("Listening for events. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
            }
        }
    }
}
