namespace NServiceBus.Routing
{
    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute
    {
        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpoint">Destination endpoint.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromEndpointName(string endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            return new UnicastRoute { Endpoint = endpoint };
        }

        /// <summary>
        /// Creates a destination based on the name of the endpoint instance.
        /// </summary>
        /// <param name="instance">Destination instance name.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromEndpointInstance(EndpointInstance instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return new UnicastRoute { Instance = instance };
        }

        /// <summary>
        /// Creates a destination based on the physical address.
        /// </summary>
        /// <param name="physicalAddress">Destination physical address.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromPhysicalAddress(string physicalAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);
            return new UnicastRoute { PhysicalAddress = physicalAddress };
        }

        private UnicastRoute()
        {
        }

        internal string Endpoint { get; private set; }
        internal EndpointInstance Instance { get; private set; }
        internal string PhysicalAddress { get; private set; }
    }
}