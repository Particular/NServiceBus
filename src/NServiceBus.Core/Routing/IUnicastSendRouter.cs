namespace NServiceBus
{
    using System;
    using Routing;

    interface IUnicastSendRouter
    {
        UnicastRoutingStrategy Route(Type messageType, IDistributionPolicy distributionPolicy);
    }
}