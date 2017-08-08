namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Settings;
    using Transport;

    /// <summary>
    /// This class provides implementers of transports with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The transport definition eg <see cref="LearningTransport" />, etc.</typeparam>
    public class TransportExtensions<T> : TransportExtensions where T : TransportDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TransportExtensions{T}" />.
        /// </summary>
        public TransportExtensions(SettingsHolder settings)
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

#if NET452
        /// <summary>
        /// Configures the transport to use the connection string with the given name.
        /// </summary>
        public new TransportExtensions<T> ConnectionStringName(string name)
        {
            base.ConnectionStringName(name);
            return this;
        }
#endif

        /// <summary>
        /// Configures the transport to use the given func as the connection string.
        /// </summary>
        public new TransportExtensions<T> ConnectionString(Func<string> connectionString)
        {
            base.ConnectionString(connectionString);
            return this;
        }

        /// <summary>
        /// Configures the transport to use a specific transaction mode.
        /// </summary>
        public new TransportExtensions<T> Transactions(TransportTransactionMode transportTransactionMode)
        {
            base.Transactions(transportTransactionMode);
            return this;
        }
    }

    /// <summary>
    /// This class provides implementers of transports with an extension mechanism for custom settings via extension methods.
    /// </summary>
    public class TransportExtensions : ExposeSettings
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TransportExtensions" />.
        /// </summary>
        public TransportExtensions(SettingsHolder settings)
            : base(settings)
        {
            settings.SetDefault<TransportConnectionString>(TransportConnectionString.Default);
        }

        /// <summary>
        /// Configures the transport to use the given string as the connection string.
        /// </summary>
        public TransportExtensions ConnectionString(string connectionString)
        {
            Guard.AgainstNullAndEmpty(nameof(connectionString), connectionString);
            Settings.Set<TransportConnectionString>(new TransportConnectionString(() => connectionString));
            return this;
        }

#if NET452
        /// <summary>
        /// Configures the transport to use the connection string with the given name.
        /// </summary>
        public TransportExtensions ConnectionStringName(string name)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Settings.Set<TransportConnectionString>(new TransportConnectionString(name));
            return this;
        }
#endif

        /// <summary>
        /// Configures the transport to use the given func as the connection string.
        /// </summary>
        public TransportExtensions ConnectionString(Func<string> connectionString)
        {
            Guard.AgainstNull(nameof(connectionString), connectionString);
            Settings.Set<TransportConnectionString>(new TransportConnectionString(connectionString));
            return this;
        }


        /// <summary>
        /// Configures the transport to use a explicit transaction mode.
        /// </summary>
        public TransportExtensions Transactions(TransportTransactionMode transportTransactionMode)
        {
            Settings.Set<TransportTransactionMode>(transportTransactionMode);
            return this;
        }
    }
}