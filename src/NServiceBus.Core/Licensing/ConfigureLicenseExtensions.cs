namespace NServiceBus
{
    using System;
    using System.IO;
    using Licensing;
    using Logging;

    /// <summary>
    /// Contains extension methods to configure license.
    /// </summary>
    static class ConfigureLicenseExtensions
    {
        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));

        /// <summary>
        /// Allows user to specify the license string.
        /// </summary>
        /// <param name="config">The current <see cref="Configure"/>.</param>
        /// <param name="licenseText">The license text.</param>
        /// <returns>The current <see cref="Configure"/>.</returns>
        public static Configure License(this Configure config, string licenseText)
        {
            if (string.IsNullOrWhiteSpace(licenseText))
            {
                throw new ArgumentException("licenseText is required", "licenseText");
            }
            Logger.Info(@"Using license supplied via fluent API.");
            LicenseManager.InitializeLicenseText(licenseText);

            return config;
        }


        /// <summary>
        /// Allows user to specify the path for the license file.
        /// </summary>
        /// <param name="config">The current <see cref="Configure"/>.</param>
        /// <param name="licenseFile">A relative or absolute path to the license file.</param>
        /// <returns>The current <see cref="Configure"/>.</returns>
        public static Configure LicensePath(this Configure config, string licenseFile)
        {

            if (!File.Exists(licenseFile))
            {
                throw new FileNotFoundException("License file not found", licenseFile);
            }

            var licenseText = NonLockingFileReader.ReadAllTextWithoutLocking(licenseFile);
            
            return config.License(licenseText);
        }
    }
}
