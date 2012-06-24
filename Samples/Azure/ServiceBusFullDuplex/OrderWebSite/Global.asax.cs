using System;
using System.Collections.Generic;
using System.Web;
using log4net;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Integration.Azure;

namespace OrderWebSite
{
    public class Global : HttpApplication
    {
        public static IBus Bus;
        public static IList<MyMessages.Order> Orders;
        private static IStartableBus _configure;

        private static readonly Lazy<IBus> StartBus = new Lazy<IBus>(ConfigureNServiceBus);

        private static IBus ConfigureNServiceBus()
        {
            var bus = _configure
                .Start();

            return bus;
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            Orders = new List<MyMessages.Order>();

           
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if(_configure == null)
                _configure = Configure.With()
               .DefaultBuilder()
               .Log4Net(new AzureAppender())
               .AzureConfigurationSource()
               .AzureServiceBusMessageQueue()
               .JsonSerializer()
               .UnicastBus()
                   .DoNotAutoSubscribe()
                   .LoadMessageHandlers()
                   .IsTransactional(true)
               .CreateBus();

            Bus = StartBus.Value;
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