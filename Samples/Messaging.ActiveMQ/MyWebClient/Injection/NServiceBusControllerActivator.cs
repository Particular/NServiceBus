namespace MyWebClient.Injection
{
    using System;
    using System.Web.Mvc;
    using System.Web.Routing;

    public class NServiceBusControllerActivator : IControllerActivator
    {
        public IController Create(RequestContext requestContext, Type controllerType)
        {
            return DependencyResolver.Current.GetService(controllerType) as IController;
        }
    }
}