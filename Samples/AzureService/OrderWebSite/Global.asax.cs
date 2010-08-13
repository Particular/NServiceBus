using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using log4net;
using log4net.Core;
using MyMessages;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Integration.Azure;
using Order = MyMessages.Order;

namespace OrderWebSite
{
    public class Global : HttpApplication
    {

        public static IBus Bus;
        public static IList<Order> Orders;

        protected void Application_Start(object sender, EventArgs e)
        {
            ConfigureNServiceBus();


            Orders = new List<Order>();
            //request all orders to "warmup" the cache
            Bus.Send(new LoadOrdersMessage());
        }

        private void ConfigureNServiceBus()
        {
            Bus = Configure.WithWeb()
                .DefaultBuilder()
                .Log4Net(new AzureAppender
                          {
                              ConnectionStringKey = "AzureQueueConfig.ConnectionString",
                              Threshold = Level.Debug
                          })
                .AzureConfigurationSource()
                .XmlSerializer()
                .UnicastBus()
                .LoadMessageHandlers()
                .AzureQueuesTransport()
                .IsTransactional(true)
                .PurgeOnStartup(true)
                .CreateBus()
                .Start();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            //get reference to the source of the exception chain
            var ex = Server.GetLastError().GetBaseException();

            LogManager.GetLogger(typeof(Global)).Error(ex.ToString());
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}