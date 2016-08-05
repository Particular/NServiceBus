namespace NServiceBus
{
    using System;
    using Routing;

    class TypeRouteSource : IRouteSource
    {
        Type messageType;
        IUnicastRoute route;

        public TypeRouteSource(Type messageType, IUnicastRoute route)
        {
            this.messageType = messageType;
            this.route = route;
        }

        public void GenerateRoutes(Action<RouteTableEntry> registerRouteCallback)
        {
            registerRouteCallback(new RouteTableEntry(messageType, route));
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Type;
    }
}