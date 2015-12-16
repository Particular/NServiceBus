namespace NServiceBus
{
    using System;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    /// <summary>
    /// Allows configuring file-based direct routing table.
    /// </summary>
    public class FileRoutingTableSettings : ExposeSettings
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public FileRoutingTableSettings(SettingsHolder settings) 
            : base(settings)
        {
        }

        /// <summary>
        /// Specifies the interval between route data refresh attempts.
        /// </summary>
        /// <param name="refreshInterval">Refresh interval.</param>
        public FileRoutingTableSettings RefreshInterval(TimeSpan refreshInterval)
        {
            Settings.Set(FileRoutingTableFeature.CheckIntervalSettingsKey, refreshInterval);
            return this;
        }

        /// <summary>
        /// Specifies the maximum number of attempts to load contents of a file before logging an error.
        /// </summary>
        /// <param name="maxLoadAttempts">Max load attepts.</param>
        public FileRoutingTableSettings MaxLoadAttempts(int maxLoadAttempts)
        {
            Settings.Set(FileRoutingTableFeature.MaxLoadAttemptsSettingsKey, maxLoadAttempts);
            return this;
        }

    }
}