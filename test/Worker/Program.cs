using System;
using NServiceBus;
using Common.Logging;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using ObjectBuilder;
using NServiceBus.Grid.MessageHandlers;

namespace Worker
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                builder.ConfigureComponent(typeof (PartnerQuoteMessageHandler), ComponentCallModelEnum.Singlecall)
                    .ConfigureProperty("MaxRandomSecondsToSleep", 5);

                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                    .UseXmlSerialization(false);

                new ConfigUnicastBus(builder)
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(GridInterceptingMessageHandler).Assembly,
                        typeof(PartnerQuoteMessageHandler).Assembly
                    );

                IBus bus = builder.Build<IBus>();
                bus.Start();

                bus.Subscribe(typeof(Messages.Event));

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
