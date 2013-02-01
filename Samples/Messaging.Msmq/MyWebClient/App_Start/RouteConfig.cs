using System.Web.Mvc;
using System.Web.Routing;

namespace MyWebClient
{
    using Handlers;

    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            
            routes.MapRoute("Default", "", new { controller = "Home", action = "Index" });

            routes.MapConnection<OrderConnection>("signalR", "/ordersShipped");

            routes.MapRoute("Others", "{controller}/{action}", new { controller = "Home", action = "Index" });
        }
    }
}