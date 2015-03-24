namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="InMemory"/>, <see cref="MsmqTransport"/>, etc</typeparam>
    public class TransportExtensions<T> : TransportExtensions where T : TransportDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public TransportExtensions(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Configures the transport to use the given string as the connection string
        /// </summary>
        public new TransportExtensions<T> ConnectionString(string connectionString)
        {
            base.ConnectionString(connectionString);
            return this;
        }

        /// <summary>
        /// Configures the transport to use the connection string with the given name
        /// </summary>
        public new TransportExtensions<T> ConnectionStringName(string name)
        {
            base.ConnectionStringName(name);
            return this;
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string
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
    public class TransportExtensions : ExposeSettings
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public TransportExtensions(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Configures the transport to use the given string as the connection string
        /// </summary>
        public TransportExtensions ConnectionString(string connectionString)
        {
            Guard.AgainstNullAndEmpty(connectionString, "connectionString");
            Settings.Set<TransportConnectionString>(new TransportConnectionString(() => connectionString));
            return this;
        }

        /// <summary>
        /// Configures the transport to use the connection string with the given name
        /// </summary>
        public TransportExtensions ConnectionStringName(string name)
        {
            Guard.AgainstNullAndEmpty(name, "name");
            Settings.Set<TransportConnectionString>(new TransportConnectionString(name));
            return this;
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string
        /// </summary>
        public TransportExtensions ConnectionString(Func<string> connectionString)
        {
            Guard.AgainstNull(connectionString, "connectionString");
            Settings.Set<TransportConnectionString>(new TransportConnectionString(connectionString));
            return this;
        }
    }

    /// <summary>
    /// Allows you to read which transport connectionstring has been set
    /// </summary>
    public static class ConfigureTransportConnectionString
    {
        /// <summary>
        /// Gets the transport connectionstring.
        /// </summary>
        public static string TransportConnectionString(this Configure config)
        {
            Guard.AgainstNull(config, "config");
            TransportConnectionString conn;
            if (config.Settings.TryGet(out conn))
            {
                return conn.GetConnectionStringOrNull();
            }
            return null;
        }
    }
}