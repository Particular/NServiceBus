namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Settings;
    using Transport;

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extension methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="InMemory" />, <see cref="MsmqTransport" />, etc.</typeparam>
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

        /// <summary>
        /// Configures the transport to use the connection string with the given name.
        /// </summary>
        public TransportExtensions ConnectionStringName(string name)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Settings.Set<TransportConnectionString>(new TransportConnectionString(name));
            return this;
        }

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

        /// <summary>
        /// Adds a rule for translating endpoint instance names to physical addresses in direct routing.
        /// </summary>
        /// <param name="rule">The rule.</param>
        public TransportExtensions AddAddressTranslationRule(Func<LogicalAddress, string> rule)
        {
            GetOrCreateTransportAddresses().AddRule(rule);
            return this;
        }

        /// <summary>
        /// Adds an exception to the translation rules for a given endpoint instance.
        /// </summary>
        /// <param name="logicalAddress">Logical address for which the exception is created.</param>
        /// <param name="transportAddress">Transport address of that instance.</param>
        public TransportExtensions AddAddressTranslationException(LogicalAddress logicalAddress, string transportAddress)
        {
            GetOrCreateTransportAddresses().AddSpecialCase(logicalAddress, transportAddress);
            return this;
        }

        /// <summary>
        /// Adds an exception to the translation rules for a given endpoint instance.
        /// </summary>
        /// <param name="endpointInstance">Endpoint instance for which the exception is created.</param>
        /// <param name="transportAddress">Transport address of that instance.</param>
        public TransportExtensions AddAddressTranslationException(EndpointInstance endpointInstance, string transportAddress)
        {
            GetOrCreateTransportAddresses().AddSpecialCase(endpointInstance, transportAddress);
            return this;
        }

        TransportAddresses GetOrCreateTransportAddresses()
        {
            TransportAddresses value;
            if (!Settings.TryGet(out value))
            {
                value = new TransportAddresses(a =>
                {
                    var transportInfrastructure = Settings.Get<TransportInfrastructure>();
                    return transportInfrastructure.ToTransportAddress(a);
                });
                Settings.Set<TransportAddresses>(value);
            }
            return value;
        }
    }

    /// <summary>
    /// Allows you to read which transport connectionstring has been set.
    /// </summary>
    public static class ConfigureTransportConnectionString
    {
        /// <summary>
        /// Gets the transport connectionstring.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "Not available any more.")]
        public static string TransportConnectionString(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}