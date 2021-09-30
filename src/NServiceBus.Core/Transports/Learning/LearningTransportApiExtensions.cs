namespace NServiceBus
{
    /// <summary>
    /// Provides support for <see cref="UseTransport{T}"/> transport APIs.
    /// </summary>
    public static class LearningTransportApiExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        [PreObsolete(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport(TransportDefinition)")]
        public static LearningTransportSettings UseTransport<T>(this EndpointConfiguration config)
          where T : LearningTransport
        {
            var transport = new LearningTransport();

            var routing = config.UseTransport(transport);

            var settings = new LearningTransportSettings(transport, routing);

            return settings;
        }
    }
}


