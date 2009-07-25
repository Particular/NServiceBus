using System;
using Common.Logging;
using FluentNHibernate.Cfg.Db;
using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Saga;
using NServiceBus.Sagas.Impl;


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
                    .SpringBuilder()
                    .XmlSerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .NHibernateSubcriptionStorage(SQLiteConfiguration.Standard.UsingFile(".\\subscriptions.sqllite"))
                    .Sagas()
                    .SQLiteSagaPersister()
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
