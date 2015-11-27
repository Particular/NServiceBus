namespace NServiceBus.Transports
{
    using NServiceBus.Settings;

    /// <summary>
    /// Provides context for configuring the transport.
    /// </summary>
    public class TransportReceivingConfigurationContext
    {
        /// <summary>
        /// Global Settings.
        /// </summary>
        public ReadOnlySettings Settings { get; }

        /// <summary>
        /// Connection string or null if a given transport does not require it.
        /// </summary>
        public string ConnectionString { get; }

        internal TransportReceivingConfigurationContext(ReadOnlySettings settings, string connectionString)
        {
            Settings = settings;
            ConnectionString = connectionString;
        }
    }
}