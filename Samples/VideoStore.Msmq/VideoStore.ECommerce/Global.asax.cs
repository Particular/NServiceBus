namespace VideoStore.ECommerce
{
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using log4net.Appender;
    using log4net.Core;
    using NServiceBus;
    using NServiceBus.Installation.Environments;

    public class MvcApplication : HttpApplication
    {
        private static IBus bus;

        private IStartableBus startableBus;

        public static IBus Bus
        {
            get { return bus; }
        }

        protected void Application_Start()
        {
            Configure.ScaleOut(s => s.UseSingleBrokerQueue());

            startableBus = Configure.With()
                .DefaultBuilder()
                .Log4Net(new DebugAppender {Threshold = Level.Warn})
                .UseTransport<Msmq>()
                .PurgeOnStartup(true)
                .UnicastBus()
                .RunHandlersUnderIncomingPrincipal(false)
                .RijndaelEncryptionService()
                .CreateBus();

            Configure.Instance.ForInstallationOn<Windows>().Install();

            bus = startableBus.Start();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_End()
        {
            startableBus.Dispose();
        }
    }
}