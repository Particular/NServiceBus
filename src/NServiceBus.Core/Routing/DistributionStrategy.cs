namespace NServiceBus.Routing
{
    /// <summary>
    /// Governs to how many and which instances of a given endpoint a message is to be sent.
    /// </summary>
    public abstract class DistributionStrategy
    {
        /// <summary>
        /// Selects destination instances from all known instances of a given endpoint.
        /// </summary>
        public abstract EndpointInstance SelectDestination(EndpointInstance[] allInstances);

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="transportAddresses"></param>
        /// <returns></returns>
        public abstract string SelectDestination(string[] transportAddresses);
    }
}