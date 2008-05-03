using System;
using NServiceBus;
using Common.Logging;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using NServiceBus.Config;
using Timeout.MessageHandlers;

namespace Timeout.Manager
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                Configure.With(builder).SagasAndMessageHandlersIn(typeof (TimeoutMessageHandler).Assembly);

                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                    .UseXmlSerialization(false);

                new ConfigUnicastBus(builder)
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(GridInterceptingMessageHandler).Assembly,
                        typeof(TimeoutMessageHandler).Assembly
                    );

                IBus bus = builder.Build<IBus>();
                bus.Start();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }

            Console.Read();
        }
    }
}
