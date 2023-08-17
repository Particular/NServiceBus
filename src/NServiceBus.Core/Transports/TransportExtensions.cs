namespace NServiceBus
{
    using Transport;

    /// <summary>
    /// This class provides implementers of transports with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The transport definition e.g. <see cref="LearningTransport" />, etc.</typeparam>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/6811",
        Message = "Configure the transport via the TransportDefinition instance's properties",
        Note = "Should not be converted to an ObsoleteEx until API mismatch described in issue is resolved.")]
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
        [PreObsolete("https://github.com/Particular/NServiceBus/issues/6811",
            ReplacementTypeOrMember = "Use EndpointConfiguration.UseTransport() to access routing settings",
            Note = "Should not be converted to an ObsoleteEx until API mismatch described in issue is resolved.")]
        public RoutingSettings<T> Routing() => routing;

        /// <summary>
        /// Configures the transport to use a specific transaction mode.
        /// </summary>
        [PreObsolete("https://github.com/Particular/NServiceBus/issues/6811",
            ReplacementTypeOrMember = "TransportDefinition.TransportTransactionMode",
            Note = "Should not be converted to an ObsoleteEx until API mismatch described in issue is resolved.")]
        public TransportExtensions<T> Transactions(TransportTransactionMode transportTransactionMode)
        {
            Transport.TransportTransactionMode = transportTransactionMode;
            return this;
        }
    }
}