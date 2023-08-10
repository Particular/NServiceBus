namespace NServiceBus
{
    using Transport;

    /// <summary>
    /// This class provides implementers of transports with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The transport definition e.g. <see cref="LearningTransport" />, etc.</typeparam>
    [PreObsolete(
        Message = "Configure the transport via the TransportDefinition instance's properties",
        TreatAsErrorFromVersion = "9",
        RemoveInVersion = "10")]
    public class TransportExtensions<T> where T : TransportDefinition
    {
        /// <summary>
        /// Instance of <see cref="TransportDefinition"/>.
        /// </summary>
        public T Transport { get; }

        readonly RoutingSettings<T> routing;

        /// <summary>
        /// Creates an instance of <see cref="TransportExtensions{T}"/>.
        /// </summary>
        public TransportExtensions(T transport, RoutingSettings<T> routing)
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
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0",
            ReplacementTypeOrMember = "TransportDefinition.TransportTransactionMode")]
        public TransportExtensions<T> Transactions(TransportTransactionMode transportTransactionMode)
        {
            Transport.TransportTransactionMode = transportTransactionMode;
            return this;
        }
    }
}