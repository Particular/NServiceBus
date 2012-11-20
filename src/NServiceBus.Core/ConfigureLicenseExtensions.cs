namespace NServiceBus
{
    using System.IO;
    using Licensing;

    /// <summary>
    /// Contains extension methods to configure license.
    /// </summary>
    public static class ConfigureLicenseExtensions
    {
        /// <summary>
        /// Allows user to specify the license string.
        /// </summary>
        /// <param name="config">The current <see cref="Configure"/>.</param>
        /// <param name="licenseText">The license text.</param>
        /// <returns>The current <see cref="Configure"/>.</returns>
        public static Configure License(this Configure config, string licenseText)
        {
            LicenseManager.Parse(licenseText);

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

            var licenseText = LicenseManager.ReadAllTextWithoutLocking(licenseFile);
            
            return config.License(licenseText);
        }
    }
}
