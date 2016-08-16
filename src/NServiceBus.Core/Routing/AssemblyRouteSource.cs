namespace NServiceBus
{
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
            return messageAssembly.GetTypes().Where(t => conventions.IsMessageType(t)).Select(t => new RouteTableEntry(t, route));
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Assembly;
    }
}