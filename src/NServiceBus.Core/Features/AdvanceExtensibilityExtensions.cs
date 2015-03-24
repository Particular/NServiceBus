namespace NServiceBus.Configuration.AdvanceExtensibility
{
    using NServiceBus.Settings;

    /// <summary>
    /// Extension methods declarations.
    /// </summary>
    public static class AdvanceExtensibilityExtensions
    {
        /// <summary>
        /// Gives access to the <see cref="SettingsHolder"/> for extensibility.
        /// </summary>
        public static SettingsHolder GetSettings(this ExposeSettings config)
        {
            Guard.AgainstNull(config, "config");
            return config.Settings;
        }
    }
}