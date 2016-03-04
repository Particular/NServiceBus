namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute : IUnicastRoute
    {
        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpoint">Destination endpoint.</param>
        public UnicastRoute(EndpointName endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            this.endpoint = endpoint;
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instance">Destination instance name.</param>
        public UnicastRoute(EndpointInstance instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            this.instance = instance;
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        public UnicastRoute(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);
            this.physicalAddress = physicalAddress;
        }

        async Task<IEnumerable<UnicastRoutingTarget>> IUnicastRoute.Resolve(Func<EndpointName, Task<IEnumerable<EndpointInstance>>> instanceResolver)
        {
            if (physicalAddress != null)
            {
                return EnumerableEx.Single(UnicastRoutingTarget.ToTransportAddress(physicalAddress));
            }
            if (instance != null)
            {
                return EnumerableEx.Single(UnicastRoutingTarget.ToEndpointInstance(instance));
            }
            var instances = await instanceResolver(endpoint).ConfigureAwait(false);
            return instances.Select(UnicastRoutingTarget.ToEndpointInstance);
        }

        EndpointName endpoint;
        EndpointInstance instance;
        string physicalAddress;
    }
}