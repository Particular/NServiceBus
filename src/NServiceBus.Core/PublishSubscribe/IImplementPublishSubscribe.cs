namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    interface IUnicastPublishSubscribe
    {
        Task Subscribe(ISubscribeContext context);

        Task Unsubscribe(IUnsubscribeContext context);

        Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType);
    }
}