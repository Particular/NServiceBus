namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute : IUnicastRoute
    {
        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpoint">Destination endpoint.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromEndpointName(string endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            return new UnicastRoute { endpoint = endpoint };
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instance">Destination instance name.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromEndpointInstance(EndpointInstance instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return new UnicastRoute { instance = instance };
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromPhysicalAddress(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);
            return new UnicastRoute { physicalAddress = physicalAddress };
        }

        private UnicastRoute()
        {
        }

        IEnumerable<UnicastRoutingTarget> IUnicastRoute.Resolve(Func<string, IEnumerable<EndpointInstance>> instanceResolver)
        {
            if (physicalAddress != null)
            {
                return new[] { UnicastRoutingTarget.ToTransportAddress(physicalAddress) };
            }
            if (instance != null)
            {
                return new[] { UnicastRoutingTarget.ToEndpointInstance(instance) };
            }
            var instances = instanceResolver(endpoint);
            return instances.Select(UnicastRoutingTarget.ToEndpointInstance);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (endpoint != null)
            {
                return endpoint;
            }
            if (instance != null)
            {
                return $"[{instance}]";
            }
            return $"<{physicalAddress}>";
        }

        string endpoint;
        EndpointInstance instance;
        string physicalAddress;
    }
}