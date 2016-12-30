namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Extensibility;
    using Routing;

    interface IUnicastPublishRouter
    {
        IEnumerable<UnicastRoutingStrategy> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag);
    }
}