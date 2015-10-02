namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;

    interface IDirectRoutingStrategy
    {
        IEnumerable<UnicastRoutingStrategy> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag);
    }
}