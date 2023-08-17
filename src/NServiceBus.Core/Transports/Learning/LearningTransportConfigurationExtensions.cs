namespace NServiceBus
{
    /// <summary>
    /// Configuration options for the learning transport.
    /// </summary>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/6811",
        Note = "Should not be converted to an ObsoleteEx until API mismatch described in issue is resolved.")]
    public static class LearningTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        [PreObsolete("https://github.com/Particular/NServiceBus/issues/6811",
            ReplacementTypeOrMember = "EndpointConfiguration.UseTransport(TransportDefinition)",
            Note = "Should not be converted to an ObsoleteEx until API mismatch described in issue is resolved.")]
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
        [PreObsolete("https://github.com/Particular/NServiceBus/issues/6811",
            ReplacementTypeOrMember = "Use LearningTransport.StorageDirectory",
            Note = "Should not be converted to an ObsoleteEx until API mismatch described in issue is resolved.")]
        public static void StorageDirectory(this TransportExtensions<LearningTransport> transportExtensions, string path)
        {
            transportExtensions.Transport.StorageDirectory = path;
        }

        /// <summary>
        /// Allows messages of any size to be sent.
        /// </summary>
        [PreObsolete("https://github.com/Particular/NServiceBus/issues/6811",
            ReplacementTypeOrMember = "Use LearningTransport.RestrictPayloadSize",
            Note = "Should not be converted to an ObsoleteEx until API mismatch described in issue is resolved.")]
        public static void NoPayloadSizeRestriction(this TransportExtensions<LearningTransport> transportExtensions)
        {
            transportExtensions.Transport.RestrictPayloadSize = false;
        }
    }
}


