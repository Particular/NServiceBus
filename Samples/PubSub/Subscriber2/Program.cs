using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.Config;
using ObjectBuilder;

namespace Subscriber2
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            NServiceBus.Config.Configure.With(builder)
                .InterfaceToXMLSerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .DoNotAutoSubscribe()
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(EventMessageHandler).Assembly
                    );


            IBus bus = builder.Build<IBus>();

            bus.Start();

            Console.WriteLine("Listening for events. To exit, press 'q' and then 'Enter'.");
            while (Console.ReadLine().ToLower() != "q")
            {
            }
        }
    }
}
