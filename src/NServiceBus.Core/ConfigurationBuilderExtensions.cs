namespace NServiceBus.AdvancedExtensibility
{
    using NServiceBus.Settings;

    /// <summary>
    /// Provides Advanced extensions for <see cref="ConfigurationBuilder"/>
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Provides access to the underlying <see cref="SettingsHolder"/> inside the <see cref="ConfigurationBuilder"/>
        /// </summary>
        public static SettingsHolder GetSettings(this ConfigurationBuilder builder)
        {
            return builder.settings;
        }
    }
}