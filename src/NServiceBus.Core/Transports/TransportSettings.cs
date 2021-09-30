namespace NServiceBus
{
    using System;
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

        /// <summary>
        /// This transport does not support a connection string.
        /// </summary>
        [PreObsolete(
            Message = "This transport does not support a connection string.",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions<T> ConnectionString(string connectionString)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This transport does not support a connection string.
        /// </summary>
        [PreObsolete(
            Message = "Setting connection string at the endpoint level is no longer supported. Transport specific configuration options should be used instead",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions<T> ConnectionString(Func<string> connectionString)
        {
            throw new NotImplementedException();
        }
    }
}


