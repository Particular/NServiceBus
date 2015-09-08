namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class DirectRoutingDestination
    {
        EndpointName endpointName;
        EndpointInstanceName instanceName;
        string physicalAddress;

        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpointName">Destination endpoint.</param>
        public DirectRoutingDestination(EndpointName endpointName)
        {
            Guard.AgainstNull("endpointName", endpointName);
            this.endpointName = endpointName;
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instanceName">Destination instance name.</param>
        public DirectRoutingDestination(EndpointInstanceName instanceName)
        {
            Guard.AgainstNull("instanceName",instanceName);
            this.instanceName = instanceName;
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        public DirectRoutingDestination(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty("physicalAddress",physicalAddress);
            this.physicalAddress = physicalAddress;
        }

        internal IEnumerable<string> Resolve(Func<EndpointName, IEnumerable<EndpointInstanceName>> instanceResolver,
            Func<IEnumerable<EndpointInstanceName>, IEnumerable<EndpointInstanceName>> instanceSelector,
            Func<EndpointInstanceName, string> addressResolver
            )
        {
            if (physicalAddress != null)
            {
                yield return physicalAddress;
            }
            else if (instanceName != null)
            {
                yield return addressResolver(instanceName);
            }
            else
            {
                var addresses = instanceSelector(instanceResolver(endpointName)).Select(addressResolver);
                foreach (var address in addresses)
                {
                    yield return address;
                }
            }
        }
    }
}