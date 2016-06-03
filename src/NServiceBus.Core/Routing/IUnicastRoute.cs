namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents destination of address routing.
    /// </summary>
    public interface IUnicastRoute
    {
        /// <summary>
        /// Resolves the destination, possibly resulting in multiple destination transport addresses.
        /// </summary>
        /// <param name="instanceResolver">A function that returns the collection of instances for a given endpoint.</param>
        Task<IEnumerable<UnicastRoutingTarget>> Resolve(Func<string, Task<IEnumerable<EndpointInstance>>> instanceResolver);
    }
}