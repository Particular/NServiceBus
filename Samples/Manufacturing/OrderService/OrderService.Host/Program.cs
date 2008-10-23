using System;
using Common.Logging;
using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Subscriptions.Msmq.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using OrderService.MessageHandlers;
using SagaPersister.OrderSagaImplementation;
using ObjectBuilder;
using NServiceBus.Saga;

namespace OrderService.Host
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Order Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                new ConfigMsmqSubscriptionStorage(builder);

                NServiceBus.Serializers.Configure.BinarySerializer.With(builder);
                //NServiceBus.Serializers.Configure.XmlSerializer.With(builder);

                new ConfigMsmqTransport(builder)
                    .IsTransactional(true)
                    .PurgeOnStartup(false);

                new ConfigUnicastBus(builder)
                    .ImpersonateSender(false)
                    .SetMessageHandlersFromAssembliesInOrder(
                        typeof(GridInterceptingMessageHandler).Assembly
                        , typeof(SagaMessageHandler).Assembly
                        , typeof(OrderSagaFinder).Assembly
                        , typeof(OrderSaga).Assembly
                    );

                builder.ConfigureComponent<OrderSagaPersister>(ComponentCallModelEnum.Singlecall);


                IBus bServer = builder.Build<IBus>();
                bServer.Start();
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
