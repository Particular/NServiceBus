namespace NServiceBus
{
    using System.Collections.Generic;
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

        public IEnumerable<RouteTableEntry> GenerateRoutes()
        {
            return messageAssembly.GetTypes().Where(t => conventions.IsMessageType(t)).Select(t => new RouteTableEntry(t, route));
        }

        public RouteSourcePriority Priority => RouteSourcePriority.Assembly;
    }
}