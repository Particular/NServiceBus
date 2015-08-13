namespace NServiceBus.Routing
{
    using System;

    class DefaultMessageRouter : MessageRouter
    {
        public DefaultMessageRouter(StaticRoutes routes)
        {
            this.routes = routes;
        }

        public override bool TryGetRoute(Type messageType, out string destination)
        {
            return routes.TryGet(messageType, out destination);
        }

        readonly StaticRoutes routes;
    }
}