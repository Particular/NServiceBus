namespace Particular.Licensing
{
    using Microsoft.Win32;

    class LicenseSourceHKCURegKey : LicenseSource
    {
        string keyPath;

        public LicenseSourceHKCURegKey(string path = @"SOFTWARE\ParticularSoftware")
            : base(LocationFriendlyName(path))
        {
            keyPath = path;
        }

        static string LocationFriendlyName(string path)
        {
            var fixedPath = path.StartsWith(@"\") ? path : @"\" + path;
            return string.Concat("HKEY_CURRENT_USER", fixedPath);
        }

        public override LicenseSourceResult Find(string applicationName)
        {
           var regLicense = ReadLicenseFromRegistry(RegistryView.Default);

            if (!string.IsNullOrWhiteSpace(regLicense))
            {
                return ValidateLicense(regLicense, applicationName);
            }

            return new LicenseSourceResult
            {
                Location = location,
                Result = $"License not found in {location}"
            };
        }

        string ReadLicenseFromRegistry(RegistryView view)
        {
            try
            {
                using (var rootKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                using (var registryKey = rootKey.OpenSubKey(keyPath))
                {
                    if (registryKey == null)
                    {
                        return null;
                    }

                    var licenseValue = registryKey.GetValue("License", null);

                    if (licenseValue is string[])
                    {
                        return string.Join(" ", (string[])licenseValue);
                    }
                    return (string)licenseValue;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
