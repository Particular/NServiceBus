using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using ObjectBuilder;
using NServiceBus.Config;
using NServiceBus.Unicast.Subscriptions.Msmq.Config;

namespace Server
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            IBuilder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                Configure.With(builder).SagasAndMessageHandlersIn(typeof(RequestDataMessageHandler).Assembly);

                new ConfigMsmqSubscriptionStorage(builder);

                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                    .UseXmlSerialization(false);

                new ConfigUnicastBus(builder)
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(RequestDataMessageHandler).Assembly
                        );

                IBus bus = builder.Build<IBus>();
                bus.Start();

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
