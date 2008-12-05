using System;
using NServiceBus;
using Common.Logging;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Config;
using ObjectBuilder;
using Timeout.MessageHandlers;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;

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
                string nameSpace = System.Configuration.ConfigurationManager.AppSettings["NameSpace"];
                string serialization = System.Configuration.ConfigurationManager.AppSettings["Serialization"];

                switch (serialization)
                {
                    case "xml":
                        builder.ConfigureComponent<MessageMapper>(ComponentCallModelEnum.Singleton);
                        builder.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                            .Namespace = nameSpace;
                        break;
                    case "binary":
                        builder.ConfigureComponent<NServiceBus.Serializers.Binary.MessageSerializer>(ComponentCallModelEnum.Singleton);
                        break;
                    default:
                        throw new ConfigurationException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
                }

                NServiceBus.Config.Configure.With(builder)
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .SetMessageHandlersFromAssembliesInOrder(
                            typeof(GridInterceptingMessageHandler).Assembly
                            , typeof(TimeoutMessageHandler).Assembly
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
