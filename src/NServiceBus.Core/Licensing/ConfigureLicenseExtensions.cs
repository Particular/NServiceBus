namespace NServiceBus
{
    using Features;

    /// <summary>
    /// Contains extension methods to configure license.
    /// </summary>
    public static class ConfigureLicenseExtensions
    {
        /// <summary>
        /// Allows user to specify the license string.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="licenseText">The license text.</param>
        public static void License(this EndpointConfiguration config, string licenseText)
        {
            Guard.AgainstNullAndEmpty(nameof(licenseText), licenseText);
            Guard.AgainstNull(nameof(config), config);

            config.Settings.Set(LicenseReminder.LicenseTextSettingsKey, licenseText);
        }

        /// <summary>
        /// Allows user to specify the path for the license file.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="licenseFile">A relative or absolute path to the license file.</param>
        public static void LicensePath(this EndpointConfiguration config, string licenseFile)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(licenseFile), licenseFile);

            config.Settings.Set(LicenseReminder.LicenseFilePathSettingsKey, licenseFile);
        }
    }
}