using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Common.Logging;
using MyMessages;
using NServiceBus;

namespace OrderWebSite
{
    public class Global : HttpApplication
    {

        public static IBus Bus;
        public static IList<Order> Orders;
        private static readonly ILog logger = LogManager.GetLogger(typeof(Global));

        protected void Application_Start(object sender, EventArgs e)
        {
            //dont add any code here, this will cause the devfabric to chooke 
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

            if (Bus != null)
            {
                return;
            }

            ConfigureLogging();
            logger.Info("Initalizing NServiceBus");
            try
            {
                Bus = Configure.WithWeb()
                .SpringBuilder()
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


            Orders = new List<Order>();
            //request all orders to "warmup" the cache
            Bus.Send(new LoadOrdersMessage());

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            //get reference to the source of the exception chain
            var ex = Server.GetLastError().GetBaseException();

            logger.Error(ex.ToString());

            //RoleManager.WriteToLog("Error",ex.ToString());

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

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