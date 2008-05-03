using System;
using Common.Logging;
using DbBlobSagaPersister.Config;
using NServiceBus;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.Msmq.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using NServiceBus.Config;
using ServerFirst;
using Server;
using NServiceBus.Saga;
using NServiceBus.Grid.MessageHandlers;
using System.Reflection;

namespace ServerRunner
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                Configure.With(builder).SagasAndMessageHandlersIn(
                    typeof(HandleCommandFirstMessageHandler).Assembly,
                    typeof(CommandMessageHandler).Assembly,
                    typeof(ISagaEntity).Assembly,
                    typeof(ChangeNumberOfWorkerThreadsMessageHandler).Assembly
                    );

                new ConfigMsmqSubscriptionStorage(builder);

                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                    .UseXmlSerialization(false);
              
                new ConfigUnicastBus(builder)
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(GridInterceptingMessageHandler).Assembly,
                        typeof(ISagaEntity).Assembly,
                        typeof(HandleCommandFirstMessageHandler).Assembly,
                        typeof(Saga).Assembly
                    );

                new ConfigSagaPersister(builder)
                    .CompletedTableName("CompletedSagas")
                    .IdColumnName("Id")
                    .ValueColumnName("Value")
                    .OnlineTableName("OnlineSagas");
                
                builder.Build<IBus>().Start();
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
