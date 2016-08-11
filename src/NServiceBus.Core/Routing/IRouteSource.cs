namespace NServiceBus
{
    using System.Collections.Generic;
    using Routing;

    interface IRouteSource
    {
        IEnumerable<RouteTableEntry> GenerateRoutes(Conventions conventions);
        RouteSourcePriority Priority { get; }
    }
}