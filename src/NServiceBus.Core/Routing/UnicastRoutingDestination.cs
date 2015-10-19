namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A destination of unicast routing.
    /// </summary>
    public sealed class UnicastRoutingDestination
    {
        EndpointName endpointName;
        EndpointInstanceName instanceName;
        string physicalAddress;

        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpointName">Destination endpoint.</param>
        public UnicastRoutingDestination(EndpointName endpointName)
        {
            Guard.AgainstNull(nameof(endpointName), endpointName);
            this.endpointName = endpointName;
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instanceName">Destination instance name.</param>
        public UnicastRoutingDestination(EndpointInstanceName instanceName)
        {
            Guard.AgainstNull(nameof(instanceName),instanceName);
            this.instanceName = instanceName;
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        public UnicastRoutingDestination(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress),physicalAddress);
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

        bool Equals(UnicastRoutingDestination other)
        {
            return Equals(endpointName, other.endpointName) && Equals(instanceName, other.instanceName) && string.Equals(physicalAddress, other.physicalAddress);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((UnicastRoutingDestination) obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (endpointName != null ? endpointName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (instanceName != null ? instanceName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (physicalAddress != null ? physicalAddress.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(UnicastRoutingDestination left, UnicastRoutingDestination right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(UnicastRoutingDestination left, UnicastRoutingDestination right)
        {
            return !Equals(left, right);
        }
    }
}