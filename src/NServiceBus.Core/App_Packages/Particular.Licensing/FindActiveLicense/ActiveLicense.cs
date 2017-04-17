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
                var selectedLicenseReportItem = $"Selected active license from {licenseSourceResultToUse.Location}";
                activeLicense.Report.Add(selectedLicenseReportItem);
                activeLicense.SelectedLicenseReport.Add(selectedLicenseReportItem);

                var details = licenseSourceResultToUse.License;
                if (details.ExpirationDate.HasValue)
                {
                    var licenseExpirationReportItem = string.Format(CultureInfo.InvariantCulture, "License Expiration: {0:dd MMMM yyyy}", details.ExpirationDate.Value);
                    activeLicense.Report.Add(licenseExpirationReportItem);
                    activeLicense.SelectedLicenseReport.Add(licenseExpirationReportItem);

                    if (details.UpgradeProtectionExpiration.HasValue)
                    {
                        var upgradeProtectionReportItem = string.Format(CultureInfo.InvariantCulture, "Upgrade Protection Expiration: {0:dd MMMM yyyy}", details.UpgradeProtectionExpiration.Value);
                        activeLicense.Report.Add(upgradeProtectionReportItem);
                        activeLicense.SelectedLicenseReport.Add(upgradeProtectionReportItem);
                    }
                }
                activeLicense.License = details;
                activeLicense.Location = licenseSourceResultToUse.Location;
            }

            if (activeLicense.License == null)
            {
                var trialStartDate = TrialStartDateStore.GetTrialStartDate();

                var trialLicenseReportItem = $"No valid license could be found, falling back to trial license with start date '{trialStartDate.ToLocalTime().ToShortDateString()}'";
                activeLicense.Report.Add(trialLicenseReportItem);
                activeLicense.SelectedLicenseReport.Add(trialLicenseReportItem);

                activeLicense.License = License.TrialLicense(trialStartDate);
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