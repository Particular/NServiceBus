namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public IEnumerable<RouteTableEntry> GenerateRoutes(Conventions conventions)
        {
            yield return new RouteTableEntry(messageType, route);
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Type;
    }
}