namespace MyWebClient
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
            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            Configure.With()
               .DefaultBuilder()
               .Log4Net(new DebugAppender{Threshold = Level.Warn})
               .ForMvc()
               .XmlSerializer()
               .UseTransport<ActiveMQ>()
                   .PurgeOnStartup(true)
               .UnicastBus()
                   .ImpersonateSender(false)
               .CreateBus()
               .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
        }
    }
}
