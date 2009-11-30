using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common.Logging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using MyMessages;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Host.Internal;
using NServiceBus.ObjectBuilder;

namespace OrderService
{
    public class WorkerRole : RoleEntryPoint
    {
        public IBus Bus;

        public OrderList orders = new OrderList();
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
                
                foreach (var order in orders.GetOrdersToApprove())
                {
                    var updatedOrder = orders.UpdateStatus(order,OrderStatus.Approved);
                    
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
                    .PurgeOnStartup(false);

                //we use inmemory storage until we have a sub storage for TableStorage or SQL Services
                Configure.Instance.Configurer.ConfigureComponent<InMemorySubscriptionStorage>(ComponentCallModelEnum.Singleton);

                Configure.Instance.Configurer.RegisterSingleton<OrderList>(orders);

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
            DiagnosticMonitor.Start(CloudStorageAccount.DevelopmentStorageAccount,new DiagnosticMonitorConfiguration());

            var props = new NameValueCollection();
            props["configType"] = "EXTERNAL";
            LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);

            var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
             var appender = new log4net.Appender.TraceAppender()
            {
                Layout = layout,
                Threshold = log4net.Core.Level.Warn
            };
            appender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(appender);

            logger = LogManager.GetLogger(typeof(WorkerRole));
        }


    }
}
