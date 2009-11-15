using System;
using System.Collections.Specialized;
using System.Threading;
using Common.Logging;
using Microsoft.ServiceHosting.ServiceRuntime;
using MyMessages;
using NServiceBus;
using NServiceBus.Host.Internal;
using NServiceBus.ObjectBuilder;
using Order=MyMessages.Order;
using RoleManager=Microsoft.ServiceHosting.ServiceRuntime.RoleManager;

namespace OrderService
{
    public class WorkerRole : RoleEntryPoint
    {
        public IBus Bus;

        public OrderList orders = new OrderList();

        public override void Start()
        {
            ConfigureLogging();
            ConfigureNServiceBus();

            while (true)
            {
                Thread.Sleep(10000);
                RoleManager.WriteToLog("Information", "Approving orders");

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
            RoleManager.WriteToLog("Information", "Worker Process entry point called");
            try
            {
                var config = Configure.With()
                    .SpringBuilder()
                    .XmlSerializer()
                    .UnicastBus()
                    .LoadMessageHandlers()
                    .AzureQueuesTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false);

                //we use inmemory storage until we have a sub storage for TableStorage or SQL Services
                Configure.Instance.Configurer.ConfigureComponent<InMemorySubscriptionStorage>(ComponentCallModelEnum.Singleton);

                Configure.Instance.Configurer.RegisterSingleton<OrderList>(orders);

                Bus = config.CreateBus()
                    .Start();

            }
            catch (Exception ex)
            {

                RoleManager.WriteToLog("Error", ex.ToString());
            }
            RoleManager.WriteToLog("Information", "NServiceBus started");
        }

        public override RoleStatus GetHealthStatus()
        {
            return RoleStatus.Healthy;
        }

        private static void ConfigureLogging()
        {
            var props = new NameValueCollection();
            props["configType"] = "EXTERNAL";
            LogManager.Adapter = new Common.Logging.Log4Net.Log4NetLoggerFactoryAdapter(props);

            var layout = new log4net.Layout.PatternLayout("%d [%t] %-5p %c [%x] <%X{auth}> - %m%n");
             var appender = new AzureAppender
            {
                Layout = layout,
                Threshold = log4net.Core.Level.Warn
            };
            appender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(appender);
        }


    }
}
