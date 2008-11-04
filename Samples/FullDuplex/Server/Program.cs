using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using ObjectBuilder;
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
                ConfigureSelfWith(builder);

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

        static void ConfigureSelfWith(IBuilder builder)
        {
            new ConfigMsmqSubscriptionStorage(builder);

            //NServiceBus.Serializers.Configure.InterfaceToXMLSerializer.WithNameSpace("http://www.UdiDahan.com").With(builder);
            NServiceBus.Serializers.Configure.XmlSerializer.WithNameSpace("http://www.UdiDahan.com").With(builder);

            new ConfigMsmqTransport(builder)
                .IsTransactional(true)
                .PurgeOnStartup(false);

            new ConfigUnicastBus(builder)
                .ImpersonateSender(false)
                .SetMessageHandlersFromAssembliesInOrder(
                    typeof(RequestDataMessageHandler).Assembly
                    );

            builder.ConfigureComponent<MessageMapper>(ComponentCallModelEnum.Singleton);
        }
    }
}
