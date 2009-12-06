using System;
using System.Collections.Specialized;
using System.Threading;
using Common.Logging;
using log4net.Core;
using Microsoft.WindowsAzure.ServiceRuntime;
using MyMessages;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Integration.Azure;

namespace OrderService
{
    public class WorkerRole : RoleEntryPoint
    {
        public IBus Bus;

        public OrderList Orders = new OrderList();
        private ILog logger;

        public override void Run()
        {
            ConfigureLogging();

            logger.Info("Starting order worker with instance id:" + RoleEnvironment.CurrentRoleInstance.Id);

            ConfigureNServiceBus();

            while (true)
            {
                Thread.Sleep(10000);

                logger.Info("Approving orders");

                foreach (var order in Orders.GetOrdersToApprove())
                {
                    var updatedOrder = Orders.UpdateStatus(order, OrderStatus.Approved);

                    //publish update
                    var orderUpdatedEvent = Bus.CreateInstance<OrderUpdatedEvent>(x => x.UpdatedOrder = updatedOrder);
                    Bus.Publish(orderUpdatedEvent);
                }
            }
        }

        private void ConfigureNServiceBus()
        {

            logger.Info("Initalizing NServiceBus");
            try
            {
                var config = Configure.With()
                    .SpringBuilder()
                    .AzureConfigurationSource()
                    .XmlSerializer()
                    .UnicastBus()
                    .LoadMessageHandlers()
                    .AzureQueuesTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                    .InMemorySubscriptionStorage();//we use inmemory storage until we have a sub storage for TableStorage or SQL Services
                
                Configure.Instance.Configurer.RegisterSingleton<OrderList>(Orders);

                Bus = config.CreateBus()
                    .Start();

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }

            logger.Info("NServiceBus started");

        }


        private void ConfigureLogging()
        {
            LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(new NameValueCollection
                                                                                            {
                                                                                                {"configType","EXTERNAL"}
                                                                                            });

            var appender = new AzureAppender
                               {
                                   ConnectionStringKey = "AzureQueueConfig.ConnectionString",
                                   Threshold = Level.Debug
                                };
            appender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(appender);

            logger = LogManager.GetLogger(typeof(WorkerRole));
        }
    }
}
