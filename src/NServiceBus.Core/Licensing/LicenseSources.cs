namespace NServiceBus
{
    using System.Collections.Generic;
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

            var standardSources = LicenseSource.GetStandardLicenseSources();

            sources.AddRange(standardSources);

#if APPCONFIGLICENSESOURCE
            sources.Add(new LicenseSourceAppConfigLicenseSetting());
            sources.Add(new LicenseSourceAppConfigLicensePathSetting());
#endif

            return sources.ToArray();
        }
    }
}