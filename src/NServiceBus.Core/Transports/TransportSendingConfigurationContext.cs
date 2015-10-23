namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Extensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides context for configuring the transport.
    /// </summary>
    public class TransportSendingConfigurationContext
    {
        Func<IDispatchMessages> dispatcherFactory;

        /// <summary>
        /// Extension settings for this transprt.
        /// </summary>
        public ContextBag ExtensionSettings { get; }

        /// <summary>
        /// Global settings.
        /// </summary>
        public ReadOnlySettings GlobalSettings { get; }

        /// <summary>
        /// Connection string or null if a given transport does not require it.
        /// </summary>
        public string ConnectionString { get; }

        internal TransportSendingConfigurationContext(ContextBag extensionSettings, ReadOnlySettings globalSettings, string connectionString)
        {
            ExtensionSettings = extensionSettings;
            this.GlobalSettings = globalSettings;
            ConnectionString = connectionString;
        }

        internal Func<IDispatchMessages> DispatcherFactory => dispatcherFactory;

        /// <summary>
        /// Configures the dispatcher factory.
        /// </summary>
        /// <param name="dispatcherFactory">Dispatcher factory.</param>
        public void SetDispatcherFactory(Func<IDispatchMessages> dispatcherFactory)
        {
            this.dispatcherFactory = dispatcherFactory;
        }
    }
}