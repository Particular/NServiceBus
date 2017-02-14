namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    interface IUnicastPublishRouter
    {
        Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, IOutgoingPublishContext publishContext);
    }
}