using System.Web.Mvc;
using System.Web.Routing;

namespace MyWebClient
{
    using Injection;
    using NServiceBus;

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            Configure.With()
               .DefaultBuilder()
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
