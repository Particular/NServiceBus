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

            if (licenseText == null && licenseFilePath == null)
            {
                var standardSources = LicenseSource.GetStandardLicenseSources();

                sources.AddRange(standardSources);
            }

            return sources.ToArray();
        }
    }
}