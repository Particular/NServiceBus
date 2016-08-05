namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Routing;

    class AssemblyRouteSource : IRouteSource
    {
        Assembly messageAssembly;
        Conventions conventions;
        IUnicastRoute route;

        public AssemblyRouteSource(Assembly messageAssembly, Conventions conventions, IUnicastRoute route)
        {
            this.messageAssembly = messageAssembly;
            this.conventions = conventions;
            this.route = route;
        }

        public void GenerateRoutes(Action<RouteTableEntry> registerRouteCallback)
        {
            foreach (var type in messageAssembly.GetTypes().Where(t => conventions.IsMessageType(t)))
            {
                registerRouteCallback(new RouteTableEntry(type, route));
            }
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Assembly;
    }
}