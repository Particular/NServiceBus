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
    public interface IUnicastPublish
    {
        /// <summary>
        /// bla.
        /// </summary>
        Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType);
    }
}