using System;
using Common.Logging;
using NServiceBus;
using Messages;

namespace Subscriber2
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
                    .DoNotAutoSubscribe()
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(EventMessageHandler).Assembly
                    )
                .CreateBus()
                .Start();

            bus.Subscribe<IEvent>();

            Console.WriteLine("Listening for events. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
            }
        }
    }
}
