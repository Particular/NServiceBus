using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.Msmq.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;

namespace Server
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                new ConfigMsmqSubscriptionStorage(builder);

                NServiceBus.Serializers.Configure.XmlSerializer.With(builder);
                
                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false);

                new ConfigUnicastBus(builder)
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(CommandMessageHandler).Assembly
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
