using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Config;
using ObjectBuilder;
using NServiceBus.Grid.MessageHandlers;

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
                ConfigureSelfWith(builder);

                var bus = builder.Build<IStartableBus>();
                bus.Start();

                Console.Read();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }

        static void ConfigureSelfWith(IBuilder builder)
        {
            NServiceBus.Config.Configure.With(builder)
                .MsmqSubscriptionStorage()
                .XmlSerializer("http://www.UdiDahan.com")
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(GridInterceptingMessageHandler).Assembly,
                        typeof(RequestDataMessageHandler).Assembly
                        );
        }
    }
}
