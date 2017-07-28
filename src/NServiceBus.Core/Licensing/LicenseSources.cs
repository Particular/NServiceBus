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

            sources.Add(new LicenseSourceConfigFile());

            sources.Add(new LicenseSourceFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NServiceBus", "License.xml")));
            sources.Add(new LicenseSourceFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License", "License.xml")));

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                sources.Add(new LicenseSourceHKCURegKey(@"SOFTWARE\ParticularSoftware"));
                sources.Add(new LicenseSourceHKLMRegKey(@"SOFTWARE\ParticularSoftware"));
            }

            return sources.ToArray();
        }
    }
}