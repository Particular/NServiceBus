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
        /// <param name="config"></param>
        /// <returns></returns>
        public static ConcurrencySettings Concurrency(this BusConfiguration config)
        {
            return new ConcurrencySettings(config);
        }
    }
}