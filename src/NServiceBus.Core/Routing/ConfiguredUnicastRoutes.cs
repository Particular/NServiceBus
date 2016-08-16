namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Routing;

    class ConfiguredUnicastRoutes
    {
        List<IRouteSource> routeSources = new List<IRouteSource>();

        public void Add(IRouteSource routeSource)
        {
            Guard.AgainstNull(nameof(routeSource), routeSource);
            routeSources.Add(routeSource);
        }

        public void Apply(UnicastRoutingTable unicastRoutingTable, Conventions conventions)
        {
            var entries = new Dictionary<Type, RouteTableEntry>();
            foreach (var source in routeSources.OrderBy(x => x.Priority)) //Higher priority routes sources override lower priority.
            {
                foreach (var route in source.GenerateRoutes(conventions))
                {
                    entries[route.MessageType] = route;
                }
            }
            unicastRoutingTable.AddOrReplaceRoutes("EndpointConfiguration", entries.Values.ToList());
        }
    }
}