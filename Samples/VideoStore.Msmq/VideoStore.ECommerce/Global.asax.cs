namespace VideoStore.ECommerce
{
    using System.Web.Mvc;
    using System.Web.Routing;
    using NServiceBus;
    using log4net.Appender;
    using log4net.Core;

    public class MvcApplication : System.Web.HttpApplication
    {
        private static IBus bus;

        protected void Application_Start()
        {
            bus = Configure.With()
                     .DefaultBuilder()
                     .Log4Net(new DebugAppender {Threshold = Level.Warn})
                     .UseTransport<Msmq>()
                     .PurgeOnStartup(true)
                     .UnicastBus()
                     .RunHandlersUnderIncomingPrincipal(false)
                     .RijndaelEncryptionService()
                     .CreateBus()
                     .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>()
                                           .Install());

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        public static IBus Bus
        {
            get { return bus; }
        }
    }
}
