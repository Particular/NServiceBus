using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Common.Logging;
using log4net.Core;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using MyMessages;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Integration.Azure;

namespace OrderWebSite
{
    public class Global : HttpApplication
    {

        public static IBus Bus;
        public static IList<Order> Orders;
        private ILog logger;

        protected void Application_Start(object sender, EventArgs e)
        {
            ConfigureLogging();
            ConfigureNServiceBus();


            Orders = new List<Order>();
            //request all orders to "warmup" the cache
            Bus.Send(new LoadOrdersMessage());
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
       
        }

        private void ConfigureNServiceBus()
        {
            logger.Info("Initalizing NServiceBus");
            try
            {
                Bus = Configure.WithWeb()
                    .SpringBuilder()
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
            catch (Exception ex)
            {
                logger.Fatal(ex.ToString());
                throw;
            }

            logger.Info("NServiceBus is started ");
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            //get reference to the source of the exception chain
            var ex = Server.GetLastError().GetBaseException();

            logger.Error(ex.ToString());
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

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

            logger = LogManager.GetLogger(typeof(Global));
        }
    }
}