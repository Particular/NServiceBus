namespace NServiceBus
{
    using System;
    using Routing;

    interface IUnicastSendRouter
    {
        UnicastRoutingStrategy Route(Type messageType, Func<string, DistributionStrategyScope, DistributionStrategy> distributionPolicy);
    }
}