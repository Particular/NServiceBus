namespace Particular.Licensing
{
    using System.Globalization;
    using System.Linq;

    class ActiveLicense
    {
        /// <summary>
        /// Compare a bunch of license sources and choose an active license
        /// </summary>
        public static ActiveLicenseFindResult Find(string applicationName, params LicenseSource[] licenseSources)
        {
            var results = licenseSources.Select(licenseSource => licenseSource.Find(applicationName)).ToList();
            var activeLicense = new ActiveLicenseFindResult();
            foreach (var result in results.Where(p => !string.IsNullOrWhiteSpace(p.Result)).Select(p => p.Result))
            {
                activeLicense.Report.Add(result);
            }

            var licenseSourceResultToUse = LicenseSourceResult.DetermineBestLicenseSourceResult(results.ToArray());
            if (licenseSourceResultToUse != null)
            {
                activeLicense.Report.Add($"Selected active license from {licenseSourceResultToUse.Location}");
                var details = licenseSourceResultToUse.License;
                if (details.ExpirationDate.HasValue)
                {
                    activeLicense.Report.Add(string.Format(CultureInfo.InvariantCulture, "License Expiration: {0:dd MMMM yyyy}", details.ExpirationDate.Value));

                    if (details.UpgradeProtectionExpiration.HasValue)
                    {
                        activeLicense.Report.Add(string.Format(CultureInfo.InvariantCulture, "Upgrade Protection Expiration: {0:dd MMMM yyyy}", details.UpgradeProtectionExpiration.Value));
                    }
                }
                activeLicense.License = details;
                activeLicense.Location = licenseSourceResultToUse.Location;
            }

            if (activeLicense.License == null)
            {
                activeLicense.Report.Add("No valid license could be found, falling back to trial license");
                activeLicense.License = License.TrialLicense(TrialStartDateStore.GetTrialStartDate());
                activeLicense.Location = "Trial License";
            }
            else if (activeLicense.License.IsTrialLicense)
            {
                // If the found license is a trial license then it is actually a extended trial license not a locally generated trial.
                // Set the property to indicate that it is an extended license as it's not set by the license generation
                activeLicense.License.IsExtendedTrial = true;
            }

            activeLicense.HasExpired = LicenseExpirationChecker.HasLicenseExpired(activeLicense.License);
            return activeLicense;
        }
    }
}