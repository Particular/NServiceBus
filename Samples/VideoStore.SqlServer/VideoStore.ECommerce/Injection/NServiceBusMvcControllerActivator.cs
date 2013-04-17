namespace VideoStore.ECommerce.Injection
{
    using System;
    using System.Web.Mvc;
    using System.Web.Routing;

    public class NServiceBusMvcControllerActivator : IControllerActivator
    {
        public IController Create(RequestContext requestContext, Type controllerType)
        {
            return DependencyResolver.Current.GetService(controllerType) as IController;
        }
    }
}