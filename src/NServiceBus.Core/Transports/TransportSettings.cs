namespace NServiceBus
{
    using Transport;

    /// <summary>
    /// Holds transports settings.
    /// </summary>
    public abstract partial class TransportSettings<T> where T : TransportDefinition
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
        [PreObsolete(
            ReplacementTypeOrMember = "Use EndpointConfiguration.UseTransport() to access routing settings",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public RoutingSettings<T> Routing() => routing;

        /// <summary>
        /// Configures the transport to use a specific transaction mode.
        /// </summary>
        [PreObsolete(
            ReplacementTypeOrMember = "TransportDefinition.TransportTransactionMode",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "9.0")]
        public TransportSettings<T> Transactions(TransportTransactionMode transportTransactionMode)
        {
            Transport.TransportTransactionMode = transportTransactionMode;
            return this;
        }
    }
}


