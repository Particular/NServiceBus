namespace NServiceBus
{
    using System.IO;
    using Logging;

    /// <summary>
    /// Contains extension methods to configure license.
    /// </summary>
    public static class ConfigureLicenseExtensions
    {
        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));

        /// <summary>
        /// Allows user to specify the license string.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
        /// <param name="licenseText">The license text.</param>
        public static void License(this EndpointConfiguration config, string licenseText)
        {
            Guard.AgainstNullAndEmpty(nameof(licenseText), licenseText);
            Guard.AgainstNull(nameof(config), config);
            Logger.Info(@"Using license supplied via fluent API.");
            config.Settings.Set(Features.LicenseReminder.LicenseTextSettingsKey, licenseText);
        }


        /// <summary>
        /// Allows user to specify the path for the license file.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
        /// <param name="licenseFile">A relative or absolute path to the license file.</param>
        public static void LicensePath(this EndpointConfiguration config, string licenseFile)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(licenseFile), licenseFile);
            if (!File.Exists(licenseFile))
            {
                throw new FileNotFoundException("License file not found", licenseFile);
            }

            var licenseText = NonLockingFileReader.ReadAllTextWithoutLocking(licenseFile);
            
            config.License(licenseText);
        }
    }
}
