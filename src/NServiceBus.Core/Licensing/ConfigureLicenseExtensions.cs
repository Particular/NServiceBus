namespace NServiceBus
{
    using System.IO;
    using Licensing;
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
        /// <param name="config">The current <see cref="BusConfiguration"/>.</param>
        /// <param name="licenseText">The license text.</param>
        public static void License(this BusConfiguration config, string licenseText)
        {
            Guard.AgainstDefaultOrEmpty(licenseText, "licenseText");
            Guard.AgainstDefault(config, "config");
            Logger.Info(@"Using license supplied via fluent API.");
            LicenseManager.InitializeLicenseText(licenseText);
        }


        /// <summary>
        /// Allows user to specify the path for the license file.
        /// </summary>
        /// <param name="config">The current <see cref="BusConfiguration"/>.</param>
        /// <param name="licenseFile">A relative or absolute path to the license file.</param>
        public static void LicensePath(this BusConfiguration config, string licenseFile)
        {
            Guard.AgainstDefault(config, "config");
            Guard.AgainstDefaultOrEmpty(licenseFile, "licenseFile");
            if (!File.Exists(licenseFile))
            {
                throw new FileNotFoundException("License file not found", licenseFile);
            }

            var licenseText = NonLockingFileReader.ReadAllTextWithoutLocking(licenseFile);
            
            config.License(licenseText);
        }
    }
}
