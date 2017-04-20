namespace Particular.Licensing
{
    using System.Security;
    using Microsoft.Win32;

    class LicenseSourceHKLMRegKey : LicenseSource
    {
        string keyPath;

        public LicenseSourceHKLMRegKey(string path = @"SOFTWARE\ParticularSoftware")
            : base(LocationFriendlyName(path))
        {
            keyPath = path;
        }

        static string LocationFriendlyName(string path)
        {
            var fixedPath = path.StartsWith(@"\") ? path : @"\" + path;
            return string.Concat("HKEY_LOCAL_MACHINE", fixedPath);
        }

        public override LicenseSourceResult Find(string applicationName)
        {
            var reg32Result = ReadFromRegistry(RegistryView.Registry32, applicationName);
            var reg64Result = ReadFromRegistry(RegistryView.Registry64, applicationName);

            return LicenseSourceResult.DetermineBestLicenseSourceResult(reg32Result, reg64Result) ?? new LicenseSourceResult
            {
                Location = location,
                Result = $"License not found in {location}"
            };
        }

        LicenseSourceResult ReadFromRegistry(RegistryView view, string applicationName)
        {
            try
            {
                using (var rootKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var registryKey = rootKey.OpenSubKey(keyPath))
                {
                    if (registryKey == null)
                    {
                        return ValidateLicense(null, applicationName);
                    }

                    var regValue = registryKey.GetValue("License", null);

                    var licenseText = (regValue is string[])
                        ? string.Join(" ", (string[])regValue)
                        : (string)regValue;

                    return ValidateLicense(licenseText, applicationName);
                }
            }
            catch (SecurityException)
            {
                return new LicenseSourceResult
                {
                    Location = location,
                    Result = $"Insufficent rights to read license from {location}"
                };
            }
        }
    }
}