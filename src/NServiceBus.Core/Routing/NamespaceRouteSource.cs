namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Routing;

    class NamespaceRouteSource : IRouteSource
    {
        Assembly messageAssembly;
        string messageNamespace;
        UnicastRoute route;

        public NamespaceRouteSource(Assembly messageAssembly, string messageNamespace, UnicastRoute route)
        {
            this.messageAssembly = messageAssembly;
            this.route = route;
            this.messageNamespace = messageNamespace;
        }

        public IEnumerable<RouteTableEntry> GenerateRoutes(Conventions conventions)
        {
            var routes = messageAssembly.GetTypes()
                .Where(t => conventions.IsMessageType(t) && string.Equals(t.Namespace, messageNamespace, StringComparison.OrdinalIgnoreCase))
                .Select(t => new RouteTableEntry(t, route))
                .ToArray();
            if (!routes.Any())
            {
                throw new Exception($"Cannot configure routing for namespace {messageNamespace} because it contains no types considered as messages. Message types have to either implement NServiceBus.IMessage interface or match a defined message convention.");
            }
            return routes;
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Namespace;
    }
}