namespace NServiceBus
{
    using System;
    using Routing;

    interface IRouteSource
    {
        void Generate(Action<RouteTableEntry> registerRouteCallback);
        RouteSourcePriority Priority { get; }
    }
}