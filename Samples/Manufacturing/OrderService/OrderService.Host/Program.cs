using System;
using Common.Logging;
using NHibernate.Cfg;
using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Saga;
using OrderService.Persistence;
using NHibernate;
using NServiceBus.ObjectBuilder;


namespace OrderService.Host
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Order Started.");

            try
            {
                var bus = NServiceBus.Configure.With()
                    .CastleWindsorBuilder()
                    .XmlSerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .DbSubscriptionStorage()
                        .Table("Subscriptions")
                        .SubscriberEndpointColumnName("SubscriberEndpoint")
                        .MessageTypeColumnName("MessageType")
                    .Sagas()
                    .NHibernateSagaPersister()
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .LoadMessageHandlers(
                            First<GridInterceptingMessageHandler>
                                .Then<SagaMessageHandler>()
                         )
                            
                    .CreateBus()
                    .Start();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
            }

            Console.Read();

        }
    }
}
