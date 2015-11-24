namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute : IUnicastRoute
    {
        EndpointName endpointName;
        EndpointInstanceName instanceName;
        string physicalAddress;

        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpointName">Destination endpoint.</param>
        public UnicastRoute(EndpointName endpointName)
        {
            Guard.AgainstNull(nameof(endpointName), endpointName);
            this.endpointName = endpointName;
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instanceName">Destination instance name.</param>
        public UnicastRoute(EndpointInstanceName instanceName)
        {
            Guard.AgainstNull(nameof(instanceName),instanceName);
            this.instanceName = instanceName;
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

        IEnumerable<UnicastRoutingTarget> IUnicastRoute.Resolve(Func<EndpointName, IEnumerable<EndpointInstanceName>> instanceResolver)
        {
            if (physicalAddress != null)
            {
                yield return UnicastRoutingTarget.ToTransportAddress(physicalAddress);
            }
            else if (instanceName != null)
            {
                yield return UnicastRoutingTarget.ToEndpointInstance(instanceName);
            }
            else
            {
                var instances = instanceResolver(endpointName);
                foreach (var instance in instances)
                {
                    yield return UnicastRoutingTarget.ToEndpointInstance(instance);
                }
            }
        }
    }
}