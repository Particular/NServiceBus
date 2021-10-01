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
        public static TransportExtensions<LearningTransport> StorageDirectory(this TransportExtensions<LearningTransport> transport, string storageDir)
        {
            transport.Transport.StorageDirectory = storageDir;

            return transport;
        }

        /// <summary>
        /// Allows messages of any size to be sent.
        /// </summary>
        [PreObsolete(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use LearningTransport.RestrictPayloadSize")]
        public static TransportExtensions<LearningTransport> NoPayloadSizeRestriction(this TransportExtensions<LearningTransport> transport)
        {
            transport.Transport.RestrictPayloadSize = false;

            return transport;
        }
    }
}


