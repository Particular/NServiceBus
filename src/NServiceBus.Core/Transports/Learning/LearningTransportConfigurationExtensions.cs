namespace NServiceBus
{
    /// <summary>
    /// Configuration options for the learning transport.
    /// </summary>
    public static class LearningTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        [PreObsolete(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport(TransportDefinition)")]
        public static TransportExtensions<LearningTransport> UseTransport<T>(this EndpointConfiguration config)
          where T : LearningTransport
        {
            var transport = new LearningTransport();

            var routing = config.UseTransport(transport);

            var settings = new TransportExtensions<LearningTransport>(transport, routing);

            return settings;
        }

        /// <summary>
        /// Configures the location where message files are stored.
        /// </summary>
        [PreObsolete(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use LearningTransport.StorageDirectory")]
        public static void StorageDirectory(this TransportExtensions<LearningTransport> transportExtensions, string path)
        {
            transportExtensions.Transport.StorageDirectory = path;
        }

        /// <summary>
        /// Allows messages of any size to be sent.
        /// </summary>
        [PreObsolete(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use LearningTransport.RestrictPayloadSize")]
        public static void NoPayloadSizeRestriction(this TransportExtensions<LearningTransport> transportExtensions)
        {
            transportExtensions.Transport.RestrictPayloadSize = false;
        }
    }
}


