using System;
using NServiceBus;
using Common.Logging;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using ObjectBuilder;
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
                string nameSpace = System.Configuration.ConfigurationManager.AppSettings["NameSpace"];
                string serialization = System.Configuration.ConfigurationManager.AppSettings["Serialization"];

                switch (serialization)
                {
                    case "interfaces":
                        builder.ConfigureComponent<MessageMapper>(ComponentCallModelEnum.Singleton);
                        NServiceBus.Serializers.Configure.InterfaceToXMLSerializer.WithNameSpace(nameSpace).With(builder);
                        break;
                    case "xml":
                        NServiceBus.Serializers.Configure.XmlSerializer.WithNameSpace(nameSpace).With(builder);
                        break;
                    case "binary":
                        NServiceBus.Serializers.Configure.BinarySerializer.With(builder);
                        break;
                    default:
                        throw new ConfigurationException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
                }
                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false);

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
