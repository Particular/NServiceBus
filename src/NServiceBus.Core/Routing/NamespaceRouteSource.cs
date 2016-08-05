namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Routing;

    class NamespaceRouteSource : IRouteSource
    {
        Assembly messageAssembly;
        string messageNamespace;
        Conventions conventions;
        IUnicastRoute route;

        public NamespaceRouteSource(Assembly messageAssembly, string messageNamespace, Conventions conventions, IUnicastRoute route)
        {
            this.messageAssembly = messageAssembly;
            this.conventions = conventions;
            this.route = route;
            this.messageNamespace = messageNamespace;
        }

        public void GenerateRoutes(Action<RouteTableEntry> registerRouteCallback)
        {
            foreach (var type in messageAssembly.GetTypes().Where(t => t.Namespace == messageNamespace).Where(t => conventions.IsMessageType(t)))
            {
                registerRouteCallback(new RouteTableEntry(type, route));
            }
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Namespace;
    }
}