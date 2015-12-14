namespace NServiceBus
{
    using System;
    using System.Configuration;
    using System.IO;
    using Logging;
    using Microsoft.Win32;

    static class LicenseLocationConventions
    {
        static ILog Logger = LogManager.GetLogger(typeof(LicenseLocationConventions));

        public static string TryFindLicenseText()
        {
            var appConfigLicenseString = ConfigurationManager.AppSettings["NServiceBus/License"];
            if (!string.IsNullOrEmpty(appConfigLicenseString))
            {
                Logger.Info(@"Using embedded license supplied via config file AppSettings/NServiceBus/License.");
                return appConfigLicenseString;
            }

            var appConfigLicenseFile = ConfigurationManager.AppSettings["NServiceBus/LicensePath"];
            if (!string.IsNullOrEmpty(appConfigLicenseFile))
            {
                if (File.Exists(appConfigLicenseFile))
                {
                    Logger.InfoFormat(@"Using license supplied via config file AppSettings/NServiceBus/LicensePath ({0}).", appConfigLicenseFile);
                    return NonLockingFileReader.ReadAllTextWithoutLocking(appConfigLicenseFile);
                }
                //TODO: should we throw if file does not exist?
                throw new Exception($"You have a configured licensing via AppConfigLicenseFile to use the file at '{appConfigLicenseFile}'. However this file does not exist. Either place a valid license at this location or remove the app setting.");
            }

            var localLicenseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"NServiceBus\License.xml");
            if (File.Exists(localLicenseFile))
            {
                Logger.InfoFormat(@"Using license in current folder ({0}).", localLicenseFile);
                return NonLockingFileReader.ReadAllTextWithoutLocking(localLicenseFile);
            }

            var oldLocalLicenseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"License\License.xml");
            if (File.Exists(oldLocalLicenseFile))
            {
                Logger.InfoFormat(@"Using license in current folder ({0}).", oldLocalLicenseFile);
                return NonLockingFileReader.ReadAllTextWithoutLocking(oldLocalLicenseFile);
            }

            var registryLicense = LoadLicenseFromRegistry();
            if (!string.IsNullOrEmpty(registryLicense))
            {
                return registryLicense;
            }

            registryLicense = LoadLicenseFromPreviousRegistryLocation("4.3");
            if (!string.IsNullOrEmpty(registryLicense))
            {
                return registryLicense;
            }

            registryLicense = LoadLicenseFromPreviousRegistryLocation("4.2");
            if (!string.IsNullOrEmpty(registryLicense))
            {
                return registryLicense;
            }

            registryLicense = LoadLicenseFromPreviousRegistryLocation("4.1");
            if (!string.IsNullOrEmpty(registryLicense))
            {
                return registryLicense;
            }

            registryLicense = LoadLicenseFromPreviousRegistryLocation("4.0");
            if (!string.IsNullOrEmpty(registryLicense))
            {
                return registryLicense;
            }

            return null;
        }

        static string LoadLicenseFromRegistry()
        {
            var hkcuLicense = GetHKCULicense(@"ParticularSoftware\NServiceBus");
            
            if (!string.IsNullOrEmpty(hkcuLicense))
            {
                Logger.Info(@"Using embedded license found in registry [HKEY_CURRENT_USER\Software\ParticularSoftware\NServiceBus\License].");

                return hkcuLicense;
            }

            var hklmLicense = GetHKLMLicense(@"ParticularSoftware\NServiceBus");
            if (!string.IsNullOrEmpty(hklmLicense))
            {
                Logger.Info(@"Using embedded license found in registry [HKEY_LOCAL_MACHINE\Software\ParticularSoftware\NServiceBus\License].");

                return hklmLicense;
            }

            return null;
        }

        static string LoadLicenseFromPreviousRegistryLocation(string version)
        {
            var hkcuLicense = GetHKCULicense(subKey: version);

            if (!string.IsNullOrEmpty(hkcuLicense))
            {
                Logger.InfoFormat(@"Using embedded license found in registry [HKEY_CURRENT_USER\Software\NServiceBus\{0}\License].", version);

                return hkcuLicense;
            }

            var hklmLicense = GetHKLMLicense(subKey: version);
            if (!string.IsNullOrEmpty(hklmLicense))
            {
                Logger.InfoFormat(@"Using embedded license found in registry [HKEY_LOCAL_MACHINE\Software\NServiceBus\{0}\License].", version);

                return hklmLicense;
            }

            return null;
        }

        static string GetHKCULicense(string softwareKey = "NServiceBus", string subKey = null)
        {
            var keyPath = @"SOFTWARE\" + softwareKey;
            
            if (subKey != null)
            {
                keyPath += @"\" + subKey;
            }

            using (var registryKey = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                return (string) registryKey?.GetValue("License", null);
            }
        }

        static string GetHKLMLicense(string softwareKey = "NServiceBus", string subKey = null)
        {
            var keyPath = @"SOFTWARE\" + softwareKey;

            if (subKey != null)
            {
                keyPath += @"\" + subKey;
            }

            try
            {
                using (var registryKey = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (registryKey != null)
                    {
                        return (string) registryKey.GetValue("License", null);
                    }
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                //Swallow exception if we can't read HKLM
            }
            return null;
        }
    }
}