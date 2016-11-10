namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    /// <summary>
    /// bla.
    /// </summary>
    public interface IUnicastPublishSubscribe
    {
        /// <summary>
        /// bla.
        /// </summary>
        Task Subscribe(ISubscribeContext context);

        /// <summary>
        /// bla.
        /// </summary>
        Task Unsubscribe(IUnsubscribeContext context);

        /// <summary>
        /// bla.
        /// </summary>
        Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType);
    }
}