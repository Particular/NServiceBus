namespace Particular.Licensing
{
    using System.Configuration;
    using System.IO;

    class LicenseSourceConfigFile : LicenseSource
    {
        public LicenseSourceConfigFile()
            : base("App config file")
        { }

        public override LicenseSourceResult Find(string applicationName)
        {
            var embeddedLicense = ReadLicenseFromAppConfig(applicationName);
            var externalLicense = ReadExternalLicense(applicationName);

            return LicenseSourceResult.DetermineBestLicenseSourceResult(embeddedLicense, externalLicense) ?? new LicenseSourceResult
            {
                Location = location,
                Result = $"License not found in {location}"
            };
        }

        LicenseSourceResult ReadLicenseFromAppConfig(string applicationName)
        {
            return ValidateLicense(ConfigurationManager.AppSettings["NServiceBus/License"], applicationName);
        }

        LicenseSourceResult ReadExternalLicense(string applicationName)
        {
            var appConfigLicenseFile = ConfigurationManager.AppSettings["NServiceBus/LicensePath"];

            if (!string.IsNullOrEmpty(appConfigLicenseFile))
            {
                if (File.Exists(appConfigLicenseFile))
                {
                    return ValidateLicense(NonBlockingReader.ReadAllTextWithoutLocking(appConfigLicenseFile), applicationName);
                }
            }
            return new LicenseSourceResult
            {
                Location = location,
                Result = $"License file not found in path supplied by app config file setting 'NServiceBus/LicensePath'.  Value was {appConfigLicenseFile}"
            };
        }
    }
}
