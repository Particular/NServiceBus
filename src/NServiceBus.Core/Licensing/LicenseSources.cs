namespace NServiceBus;

using System.Collections.Generic;
using Particular.Licensing;

static class LicenseSources
{
    public static LicenseSource[] GetLicenseSources(string licenseText, string licenseFilePath)
    {
        var sources = new List<LicenseSource>();

        if (!string.IsNullOrEmpty(licenseText))
        {
            sources.Add(new LicenseSourceUserProvided(licenseText));
        }

        if (!string.IsNullOrEmpty(licenseFilePath))
        {
            sources.Add(new LicenseSourceFilePath(licenseFilePath));
        }

        if (licenseText == null && licenseFilePath == null)
        {
            // TODO: When a user invokes either endpointConfiguration.LicensePath() or . License(string) with a null value
            //       this would now probe file locations for a valid license which I think is unwanted.
            var standardSources = LicenseSource.GetStandardLicenseSources();

            sources.AddRange(standardSources);
        }

        return sources.ToArray();
    }
}