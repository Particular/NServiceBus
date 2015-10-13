namespace NServiceBus
{
    using System;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="InMemory"/>, <see cref="MsmqTransport"/>, etc.</typeparam>
    public class TransportExtensions<T> : TransportExtensions where T : TransportDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TransportExtensions{T}"/>.
        /// </summary>
        public TransportExtensions(ContextBag settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Configures the transport to use the given string as the connection string.
        /// </summary>
        public new TransportExtensions<T> ConnectionString(string connectionString)
        {
            base.ConnectionString(connectionString);
            return this;
        }

        /// <summary>
        /// Configures the transport to use the connection string with the given name.
        /// </summary>
        public new TransportExtensions<T> ConnectionStringName(string name)
        {
            base.ConnectionStringName(name);
            return this;
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string.
        /// </summary>
        public new TransportExtensions<T> ConnectionString(Func<string> connectionString)
        {
            base.ConnectionString(connectionString);
            return this;
        }
    }

    /// <summary>
    /// This class provides implementers of transports with an extension mechanism for custom settings via extention methods.
    /// </summary>
    public class TransportExtensions
    {
        /// <summary>
        /// Allows accessing the settings for this transport.
        /// </summary>
        public ContextBag Settings { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TransportExtensions"/>.
        /// </summary>
        public TransportExtensions(ContextBag settings)
        {
            Settings = settings;
            settings.Set(TransportConnectionString.Default);
        }

        /// <summary>
        /// Configures the transport to use the given string as the connection string.
        /// </summary>
        public TransportExtensions ConnectionString(string connectionString)
        {
            Guard.AgainstNullAndEmpty(nameof(connectionString), connectionString);
            Settings.Set(new TransportConnectionString(() => connectionString));
            return this;
        }

        /// <summary>
        /// Configures the transport to use the connection string with the given name.
        /// </summary>
        public TransportExtensions ConnectionStringName(string name)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Settings.Set(new TransportConnectionString(name));
            return this;
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string.
        /// </summary>
        public TransportExtensions ConnectionString(Func<string> connectionString)
        {
            Guard.AgainstNull(nameof(connectionString), connectionString);
            Settings.Set(new TransportConnectionString(connectionString));
            return this;
        }
    }
}