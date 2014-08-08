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
        SettingsHolder settings;

        internal TransportConfiguration(SettingsHolder settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Configures the transport to use the given string as the connection string
        /// </summary>
        public void ConnectionString(string connectionString)
        {
            settings.Set<TransportConnectionString>(new TransportConnectionString(() => connectionString));
        }

        /// <summary>
        /// Configures the transport to use the connection string with the given name
        /// </summary>
        public void ConnectionStringName(string name)
        {
            settings.Set<TransportConnectionString>(new TransportConnectionString(name));
        }

        /// <summary>
        /// Configures the transport to use the given func as the connection string
        /// </summary>
        public void ConnectionString(Func<string> connectionString)
        {
            settings.Set<TransportConnectionString>(new TransportConnectionString(connectionString));
        }
    }
}