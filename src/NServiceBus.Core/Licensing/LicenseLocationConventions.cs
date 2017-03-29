namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Particular.Licensing;

    static class LicenseLocationConventions
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

            sources.Add(new LicenseSourceFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"NServiceBus\License.xml")));
            sources.Add(new LicenseSourceFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"License\License.xml")));

            sources.Add(new LicenseSourceHKCURegKey(@"SOFTWARE\ParticularSoftware"));
            sources.Add(new LicenseSourceHKLMRegKey(@"SOFTWARE\ParticularSoftware"));

            sources.Add(new LicenseSourceHKCURegKey(@"SOFTWARE\ParticularSoftware\NServiceBus"));
            sources.Add(new LicenseSourceHKLMRegKey(@"SOFTWARE\ParticularSoftware\NServiceBus"));

            sources.Add(new LicenseSourceHKCURegKey(@"SOFTWARE\NServiceBus\4.3"));
            sources.Add(new LicenseSourceHKLMRegKey(@"SOFTWARE\NServiceBus\4.3"));

            sources.Add(new LicenseSourceHKCURegKey(@"SOFTWARE\NServiceBus\4.2"));
            sources.Add(new LicenseSourceHKLMRegKey(@"SOFTWARE\NServiceBus\4.2"));

            sources.Add(new LicenseSourceHKCURegKey(@"SOFTWARE\NServiceBus\4.1"));
            sources.Add(new LicenseSourceHKLMRegKey(@"SOFTWARE\NServiceBus\4.1"));

            sources.Add(new LicenseSourceHKCURegKey(@"SOFTWARE\NServiceBus\4.0"));
            sources.Add(new LicenseSourceHKLMRegKey(@"SOFTWARE\NServiceBus\4.0"));

            return sources.ToArray();
        }
    }
}