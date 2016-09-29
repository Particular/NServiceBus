namespace NServiceBus.Routing
{
    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute
    {
        private UnicastRoute()
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

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (Endpoint != null)
            {
                return Endpoint;
            }
            if (Instance != null)
            {
                return $"[{Instance}]";
            }
            return $"<{PhysicalAddress}>";
        }
    }
}