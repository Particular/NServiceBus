using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Integration.Azure;

namespace Host
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static IBus Bus;

        private static readonly Lazy<IBus> StartBus = new Lazy<IBus>(ConfigureNServiceBus);

        private static IBus ConfigureNServiceBus()
        {
            Configure.Serialization.Json();
            var bus = Configure.With()
                  .DefaultBuilder()
                  .ForMvc()
                  .Log4Net(new AzureAppender())
                  .AzureConfigurationSource()
                  .AzureMessageQueue()
                    .QueuePerInstance()
                  .UnicastBus()
                    .LoadMessageHandlers()
                    .IsTransactional(true)
                  .CreateBus()
                  .Start();

            return bus;
        }

        protected void Application_BeginRequest()
        {
            Bus = StartBus.Value;
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
               "Hello", // Route name
               "Hello", // URL with parameters
               new { controller = "Home", action = "Hello" } // Parameter defaults
           );

            routes.MapRoute(
              "Text", // Route name
              "Text", // URL with parameters
              new { controller = "Home", action = "Text" } // Parameter defaults
          );

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}