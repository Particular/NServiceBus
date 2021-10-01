namespace NServiceBus
{
    using System;
    using Settings;
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
        /// Initializes a new instance of <see cref="TransportExtensions{T}" />.
        /// </summary>
        [ObsoleteEx(
            Message = "TransportExtensions does not use a SettingsHolder. Get an instance from endpointConfiguration.UseTransport<TTransport>(), or configure the transport directly via the TransportDefinition instance's properties.",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions(SettingsHolder settings)
        {
            throw new NotImplementedException();
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
        /// Configures the transport to use the connection string with the given name.
        /// </summary>
        [ObsoleteEx(
            Message = "Loading named connection strings is no longer supported",
            ReplacementTypeOrMember = "ConnectionString(connectionString)",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions<T> ConnectionStringName(string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string.
        /// </summary>
        [ObsoleteEx(
            Message = "This transport does not support a connection string.",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions<T> ConnectionString(string connectionString)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string.
        /// </summary>
        [ObsoleteEx(
            Message = "This transport does not support a connection string.",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public TransportExtensions<T> ConnectionString(Func<string> connectionString)
        {
            throw new NotImplementedException();
        }

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