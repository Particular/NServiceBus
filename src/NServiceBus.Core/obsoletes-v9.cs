namespace NServiceBus
{
    using Transport;

    /// <summary>
    /// Provides support for <see cref="UseTransport{T}"/> transport APIs.
    /// </summary>
    public static class LearningTransportApiExtensions
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
    public class LearningTransportSettings : TransportSettings<LearningTransport>
    {
        internal LearningTransportSettings(LearningTransport transport, RoutingSettings<LearningTransport> routing)
            : base(transport, routing)

        {
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
            Transport.StorageDirectory = storageDir;

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
            Transport.RestrictPayloadSize = false;

            return this;
        }
    }

    /// <summary>
    /// Holds transports settings.
    /// </summary>
    public abstract class TransportSettings<T> where T : TransportDefinition
    {
        /// <summary>
        /// Instance of <see cref="TransportDefinition"/>.
        /// </summary>
        protected readonly T Transport;

        readonly RoutingSettings<T> routing;

        /// <summary>
        /// Creates an instance of <see cref="TransportSettings{T}"/>.
        /// </summary>
        protected TransportSettings(T transport, RoutingSettings<T> routing)
        {
            Transport = transport;
            this.routing = routing;
        }

        /// <summary>
        /// Routing configuration.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "10",
            TreatAsErrorFromVersion = "9",
            ReplacementTypeOrMember = "Use EndpointConfiguration.UseTransport() to access routing settings")]
        public RoutingSettings<T> Routing() => routing;
    }
}


