namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute : IUnicastRoute
    {
        Endpoint endpoint;
        EndpointInstance instance;
        string physicalAddress;

        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpoint">Destination endpoint.</param>
        public UnicastRoute(Endpoint endpoint)
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
            Guard.AgainstNull(nameof(instance),instance);
            this.instance = instance;
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        public UnicastRoute(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress),physicalAddress);
            this.physicalAddress = physicalAddress;
        }

        IEnumerable<UnicastRoutingTarget> IUnicastRoute.Resolve(Func<Endpoint, IEnumerable<EndpointInstance>> instanceResolver)
        {
            if (physicalAddress != null)
            {
                yield return UnicastRoutingTarget.ToTransportAddress(physicalAddress);
            }
            else if (instance != null)
            {
                yield return UnicastRoutingTarget.ToEndpointInstance(instance);
            }
            else
            {
                var instances = instanceResolver(endpoint);
                foreach (var i in instances)
                {
                    yield return UnicastRoutingTarget.ToEndpointInstance(i);
                }
            }
        }
    }
}