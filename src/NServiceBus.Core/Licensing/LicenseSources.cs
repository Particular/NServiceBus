namespace NServiceBus
{
    using Particular.Licensing;

    static class LicenseSources
    {
        public static LicenseSource[] GetLicenseSources(string licenseText, string licenseFilePath)
        {
            var sources = LicenseSource.GetStandardLicenseSources();

            if (licenseText != null)
            {
                sources.Add(new LicenseSourceUserProvided(licenseText));
            }

            if (licenseFilePath != null)
            {
                sources.Add(new LicenseSourceFilePath(licenseFilePath));
            }

#if APPCONFIGLICENSESOURCE
            sources.Add(new LicenseSourceAppConfigLicenseSetting());
            sources.Add(new LicenseSourceAppConfigLicensePathSetting());
#endif

            return sources.ToArray();
        }
    }
}