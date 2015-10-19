namespace NServiceBus.Transports
{
    using NServiceBus.Routing;

    /// <summary>
    /// Allows to specify transport in routing.
    /// </summary>
    public static class EndpointInstanceDataExtensions
    {
        /// <summary>
        /// Instructs the routing to use specified transport when sending to this endpoint instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instanceData"></param>
        public static EndpointInstanceData UseTransport<T>(this EndpointInstanceData instanceData)
            where T : TransportDefinition, new()
        {
            instanceData.ExtensionData["NServiceBus.Transports.SpecificTransport"] = typeof(T).AssemblyQualifiedName;
            return instanceData;
        }
    }
}