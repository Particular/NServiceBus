namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Particular.Licensing;

    static class LicenseSources
    {
        public static LicenseSource[] GetLicenseSources(string licenseText, string licenseFilePath)
        {
            var sources = new List<LicenseSource>();

            if (licenseText != null)
            {
                sources.Add(new LicenseSourceUserProvided(licenseText));
            }

            if (licenseFilePath != null)
            {
                sources.Add(new LicenseSourceFilePath(licenseFilePath));
            }

            sources.Add(new LicenseSourceAppConfigLicenseSetting());
            sources.Add(new LicenseSourceAppConfigLicensePathSetting());

            sources.Add(new LicenseSourceFilePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ParticularSoftware", "license.xml")));
            sources.Add(new LicenseSourceFilePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParticularSoftware", "license.xml")));
            sources.Add(new LicenseSourceFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.xml")));

            return sources.ToArray();
        }
    }
}