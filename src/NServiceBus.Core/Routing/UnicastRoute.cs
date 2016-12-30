namespace NServiceBus.Routing
{
    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute
    {
        UnicastRoute()
        {
        }

        /// <summary>
        /// The logical endpoint name if present.
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// The endpoint instance if present.
        /// </summary>
        public EndpointInstance Instance { get; private set; }

        /// <summary>
        /// The physical address if present.
        /// </summary>
        public string PhysicalAddress { get; private set; }

        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpoint">Destination endpoint.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromEndpointName(string endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            return new UnicastRoute
            {
                Endpoint = endpoint
            };
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instance">Destination instance name.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromEndpointInstance(EndpointInstance instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return new UnicastRoute
            {
                Instance = instance
            };
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromPhysicalAddress(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);
            return new UnicastRoute
            {
                PhysicalAddress = physicalAddress
            };
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        /// <param name="endpoint">Optional logical endpoint name.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromPhysicalAddress(string physicalAddress, string endpoint)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);
            return new UnicastRoute
            {
                Endpoint = endpoint,
                PhysicalAddress = physicalAddress
            };
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (Instance != null)
            {
                return $"[{Instance}]";
            }
            if (PhysicalAddress != null)
            {
                return $"<{PhysicalAddress}>{Endpoint ?? ""}";
            }
            return Endpoint;
        }

        bool Equals(UnicastRoute other)
        {
            return string.Equals(Endpoint, other.Endpoint) && Equals(Instance, other.Instance) && string.Equals(PhysicalAddress, other.PhysicalAddress);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnicastRoute) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Endpoint != null ? Endpoint.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Instance != null ? Instance.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (PhysicalAddress != null ? PhysicalAddress.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}