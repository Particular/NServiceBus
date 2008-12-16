using System;
using Common.Logging;
using NServiceBus;

namespace Server
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");

            try
            {
                var bus = NServiceBus.Configure.With()
                    .SpringBuilder()
                    .XmlSerializer()
                    .MsmqSubscriptionStorage()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .SetMessageHandlersFromAssembliesInOrder(
                            typeof(CommandMessageHandler).Assembly
                            )
                    .CreateBus()
                    .Start();

                Console.Read();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }
    }
}
