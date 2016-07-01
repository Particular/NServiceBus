namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Features;
    using Settings;

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
        /// The default value is 30 seconds.
        /// Valid values must be between 1 second and less than 1 day.
        /// </summary>
        /// <param name="refreshInterval">Refresh interval.</param>
        public FileRoutingTableSettings RefreshInterval(TimeSpan refreshInterval)
        {
            if (refreshInterval < TimeSpan.FromSeconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(refreshInterval), "Value must be at least 1 second.");
            }
            if (refreshInterval > TimeSpan.FromDays(1))
            {
                throw new ArgumentOutOfRangeException(nameof(refreshInterval), "Value must be less than 1 day.");
            }
            Settings.Set(FileRoutingTableFeature.CheckIntervalSettingsKey, refreshInterval);
            return this;
        }

        /// <summary>
        /// Specifies the maximum number of attempts to load contents of a file before logging an error.
        /// The default value is 10 attempts.
        /// Valid values must be at least 1.
        /// </summary>
        /// <param name="maxLoadAttempts">Max load attempts.</param>
        public FileRoutingTableSettings MaxLoadAttempts(int maxLoadAttempts)
        {
            if (maxLoadAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLoadAttempts), "Value must be at least 0.");
            }
            Settings.Set(FileRoutingTableFeature.MaxLoadAttemptsSettingsKey, maxLoadAttempts);
            return this;
        }
    }
}