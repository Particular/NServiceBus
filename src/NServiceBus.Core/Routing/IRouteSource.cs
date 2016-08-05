namespace NServiceBus
{
    using System;
    using Routing;

    interface IRouteSource
    {
        void GenerateRoutes(Action<RouteTableEntry> registerRouteCallback);
        RouteSourcePriority Priority { get; }
    }
}