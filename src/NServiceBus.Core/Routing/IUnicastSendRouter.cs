namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Routing;

    interface IUnicastSendRouter
    {
        IEnumerable<UnicastRoutingStrategy> Route(Type messageType, IDistributionPolicy distributionPolicy);
    }
}