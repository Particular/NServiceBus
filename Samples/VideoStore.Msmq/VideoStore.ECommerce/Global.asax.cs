namespace VideoStore.ECommerce
{
    using System.Web.Mvc;
    using System.Web.Routing;
    using Injection;
    using NServiceBus;
    using log4net.Appender;
    using log4net.Core;

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Configure.With()
                     .DefaultBuilder()
                     .Log4Net(new DebugAppender {Threshold = Level.Warn})
                     .ModifyMvcAndSignalRToUseOurContainer()
                     .XmlSerializer()
                     .UseTransport<Msmq>()
                     .PurgeOnStartup(true)
                     .UnicastBus()
                     .RunHandlersUnderIncomingPrincipal(false)
                     .CreateBus()
                     .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>()
                                           .Install());

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
