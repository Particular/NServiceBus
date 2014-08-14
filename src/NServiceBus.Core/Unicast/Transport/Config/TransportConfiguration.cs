namespace NServiceBus
{
    using System;
    using NServiceBus.Settings;
    using Unicast.Transport;

    /// <summary>
    /// Configuration options common for all transports
    /// </summary>
    public class TransportConfiguration
    {
        /// <summary>
        /// Access to the current <see cref="SettingsHolder"/> instance.
        /// </summary>
        public SettingsHolder Settings { get; private set; }

        internal TransportConfiguration(SettingsHolder settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Configures the transport to use the given string as the connection string
        /// </summary>
        public void ConnectionString(string connectionString)
        {
            Settings.Set<TransportConnectionString>(new TransportConnectionString(() => connectionString));
        }

        /// <summary>
        /// Configures the transport to use the connection string with the given name
        /// </summary>
        public void ConnectionStringName(string name)
        {
            Settings.Set<TransportConnectionString>(new TransportConnectionString(name));
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string
        /// </summary>
        public void ConnectionString(Func<string> connectionString)
        {
            Settings.Set<TransportConnectionString>(new TransportConnectionString(connectionString));
        }
    }
}