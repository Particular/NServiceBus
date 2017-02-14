namespace NServiceBus
{
    using System;
    using Pipeline;
    using Routing;

    interface IUnicastSendRouter
    {
        UnicastRoutingStrategy Route(Type messageType, IDistributionPolicy distributionPolicy, IOutgoingSendContext sendContext);
    }
}