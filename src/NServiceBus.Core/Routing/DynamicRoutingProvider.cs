namespace NServiceBus.Routing
{
    /// <summary>
    /// Wraps the <see cref="IProvideDynamicRouting"/> in a convenient injectable class.
    /// </summary>
    public class DynamicRoutingProvider
    {
        /// <summary>
        /// The registered <see cref="IProvideDynamicRouting"/> impl.
        /// </summary>
        public IProvideDynamicRouting DynamicRouting { get; set; }
     
        /// <summary>
        /// Returns the full address to send messages to based on the logical <paramref name="endpoint"/> provided.
        /// If no routing distribution is available for the <paramref name="endpoint"/>, the logical endpoint is returned.
        /// </summary>
        /// <param name="endpoint">The logical endpoint to get a dynamic address for.</param>
        /// <returns>The full address to send to.</returns>
        public string GetRouteAddress(string endpoint)
        {
            string dynamicAddress;
            if (DynamicRouting != null && DynamicRouting.TryGetRouteAddress(endpoint, out dynamicAddress))
            {
                return dynamicAddress;
            }

            return endpoint;
        }
    }
}