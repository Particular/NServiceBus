namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Routing;

    class NamespaceRouteSource : IRouteSource
    {
        Assembly messageAssembly;
        string messageNamespace;
        IUnicastRoute route;

        public NamespaceRouteSource(Assembly messageAssembly, string messageNamespace, IUnicastRoute route)
        {
            this.messageAssembly = messageAssembly;
            this.route = route;
            this.messageNamespace = messageNamespace;
        }

        public IEnumerable<RouteTableEntry> GenerateRoutes(Conventions conventions)
        {
            return messageAssembly.GetTypes().Where(t => t.Namespace == messageNamespace).Where(t => conventions.IsMessageType(t)).Select(t => new RouteTableEntry(t, route));
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Namespace;
    }
}