namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Routing;

    class TypeRouteSource : IRouteSource
    {
        Type messageType;
        UnicastRoute route;

        public TypeRouteSource(Type messageType, UnicastRoute route)
        {
            this.messageType = messageType;
            this.route = route;
        }

        public IEnumerable<RouteTableEntry> GenerateRoutes(Conventions conventions)
        {
            if (!conventions.IsMessageType(messageType))
            {
                throw new Exception($"Cannot configure routing for type '{messageType.FullName}' because it is not considered a message. Message types have to either implement NServiceBus.IMessage interface or match a defined message convention.");
            }
            yield return new RouteTableEntry(messageType, route);
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Type;
    }
}