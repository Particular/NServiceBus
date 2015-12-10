namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents destination of address routing.
    /// </summary>
    public interface IUnicastRoute
    {
        /// <summary>
        /// Resolves the destination, possibly resulting in multiple destination transport addresses.
        /// </summary>
        /// <param name="instanceResolver">A function that returns the collection of instances for a given endpoint.</param>
        IEnumerable<UnicastRoutingTarget> Resolve(Func<Endpoint, IEnumerable<EndpointInstance>> instanceResolver);
    }
}