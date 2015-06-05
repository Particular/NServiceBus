namespace NServiceBus
{
    using NServiceBus.Settings.Throttling;

    /// <summary>
    /// Allows users to fine-control NServiceBus throttling settings.
    /// </summary>
    public static class ThrottlingSettingsExtensions
    {
        /// <summary>
        /// Entry point for throttling configuration.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static ThrottlingSettings Throttling(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            return new ThrottlingSettings(config);
        }
    }
}