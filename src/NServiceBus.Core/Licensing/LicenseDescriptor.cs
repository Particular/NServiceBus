namespace NServiceBus.Licensing
{
    using System;
    using System.Configuration;
    using System.IO;
    using Microsoft.Win32;

    public class LicenseDescriptor
    {
        public static string OldLocalLicenseFile
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"License\License.xml");
            }
        }

        public static string LocalLicenseFile
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"NServiceBus\License.xml");
            }
        }

        public static string HKCULicense
        {
            get
            {
                using (var registryKey = Registry.CurrentUser.OpenSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", LicenseManager.SoftwareVersion.ToString(2))))
                {
                    if (registryKey != null)
                    {
                        return (string)registryKey.GetValue("License", null);
                    }
                }

                return null;
            }
        }

        public static string HKLMLicense
        {
            get
            {
                try
                {
                    using (var registryKey = Registry.LocalMachine.OpenSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", LicenseManager.SoftwareVersion.ToString(2))))
                    {
                        if (registryKey != null)
                        {
                            return (string) registryKey.GetValue("License", null);
                        }
                    }
                }
                catch (Exception)
                {
                    //Swallow exception if we can't read HKLM
                }

                return null;
            }
        }

        public static string AppConfigLicenseFile
        {
            get { return ConfigurationManager.AppSettings["NServiceBus/LicensePath"]; }
        }

        public static string AppConfigLicenseString
        {
            get { return ConfigurationManager.AppSettings["NServiceBus/License"]; }
        }

        public static string PublicKey
        {
            get
            {
                return @"<RSAKeyValue><Modulus>5M9/p7N+JczIN/e5eObahxeCIe//2xRLA9YTam7zBrcUGt1UlnXqL0l/8uO8rsO5tl+tjjIV9bOTpDLfx0H03VJyxsE8BEpSVu48xujvI25+0mWRnk4V50bDZykCTS3Du0c8XvYj5jIKOHPtU//mKXVULhagT8GkAnNnMj9CvTc=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
            }
        }
    }
}