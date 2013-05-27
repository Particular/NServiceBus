using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NServiceBus;

namespace Website
{
    public class MvcApplication : HttpApplication
    {
        public static IBus Bus;

        private static readonly Lazy<IBus> StartBus = new Lazy<IBus>(ConfigureNServiceBus);

        private static IBus ConfigureNServiceBus()
        {
            Configure.Transactions.Enable();
            Configure.Serialization.Json();

            var bus = Configure.With()
                  .DefaultBuilder()
                  .ForMvc()
                  .AzureDiagnosticsLogger()
                  .AzureConfigurationSource()
                  .AzureServiceBusMessageQueue()
                    .QueuePerInstance()
                    .PurgeOnStartup(true)
                  .UseInMemoryTimeoutPersister()
                  .UnicastBus()
                      .LoadMessageHandlers()
                  .CreateBus()
                    .Start();

            return bus;
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "SendLinks", id = UrlParameter.Optional } // Parameter defaults

            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Bus = StartBus.Value;
        }
    }
}