namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Routing;

    class AssemblyRouteSource : IRouteSource
    {
        Assembly messageAssembly;
        UnicastRoute route;

        public AssemblyRouteSource(Assembly messageAssembly, UnicastRoute route)
        {
            this.messageAssembly = messageAssembly;
            this.route = route;
        }

        public IEnumerable<RouteTableEntry> GenerateRoutes(Conventions conventions)
        {
            var routes = messageAssembly.GetTypes()
                .Where(t => conventions.IsMessageType(t))
                .Select(t => new RouteTableEntry(t, route))
                .ToArray();

            if (!routes.Any())
            {
                throw new Exception($"Cannot configure routing for assembly {messageAssembly.GetName().Name} because it contains no types considered as messages. Message types have to either implement NServiceBus.IMessage interface or match a defined message convention.");
            }

            return routes;
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Assembly;
    }
}