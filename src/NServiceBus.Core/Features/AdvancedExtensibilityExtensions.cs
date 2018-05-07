namespace NServiceBus.Configuration.AdvancedExtensibility
{
    using Settings;

    /// <summary>
    /// Extension methods declarations.
    /// </summary>
    public static class AdvancedExtensibilityExtensions
    {
        /// <summary>
        /// Gives access to the <see cref="SettingsHolder" /> for extensibility.
        /// </summary>
        public static SettingsHolder GetSettings(this ExposeSettings config)
        {
            Guard.AgainstNull(nameof(config), config);
            return config.Settings;
        }
    }
}