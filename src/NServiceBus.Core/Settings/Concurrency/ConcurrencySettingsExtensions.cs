namespace NServiceBus
{
    using NServiceBus.Settings.Concurrency;

    /// <summary>
    /// Allows users to fine-control NServiceBus concurrency settings.
    /// </summary>
    public static class ConcurrencySettingsExtensions
    {
        /// <summary>
        /// Entry point for concurrency configuration.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static ConcurrencySettings Concurrency(this BusConfiguration config)
        {
            Guard.AgainstNull("config", config);
            return new ConcurrencySettings(config);
        }
    }
}