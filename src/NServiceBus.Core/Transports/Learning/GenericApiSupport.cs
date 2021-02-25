namespace NServiceBus
{
    /// <summary>
    /// Provides support for <see cref="UseTransport{T}"/> transport APIs.
    /// </summary>
    public static class GenericApiExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        [ObsoleteEx(
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

    /// <summary>
    /// Learning transport configuration settings.
    /// </summary>
    public class LearningTransportSettings
    {
        readonly LearningTransport transport;
        readonly RoutingSettings<LearningTransport> routing;

        internal LearningTransportSettings(LearningTransport transport, RoutingSettings<LearningTransport> routing)
        {
            this.transport = transport;
            this.routing = routing;
        }

        /// <summary>
        /// Configures the location where message files are stored.
        /// </summary>
        /// <param name="storageDir">The storage path.</param>
        [ObsoleteEx(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use LearningTransport.StorageDirectory")]
        public LearningTransportSettings StorageDirectory(string storageDir)
        {
            transport.StorageDirectory = storageDir;

            return this;
        }

        /// <summary>
        /// Allows messages of any size to be sent.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use LearningTransport.RestrictPayloadSize")]
        public LearningTransportSettings NoPayloadSizeRestriction()
        {
            transport.RestrictPayloadSize = false;

            return this;
        }

        /// <summary>
        /// Configures the routing.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use EndpointConfiguration.UseTransport() to access routing settings")]
        public RoutingSettings<LearningTransport> Routing() => routing;
    }
}


