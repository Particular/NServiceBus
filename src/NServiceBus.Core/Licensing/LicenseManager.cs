namespace NServiceBus
{
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Forms;
    using Logging;
    using Particular.Licensing;

    class LicenseManager
    {
        internal bool HasLicenseExpired()
        {
            return license == null || LicenseExpirationChecker.HasLicenseExpired(license);
        }

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            var result = ActiveLicense.Find("NServiceBus", licenseSources);
            license = result.License;

            if (result.HasExpired)
            {
                if (license.IsTrialLicense)
                {
                    Logger.WarnFormat("Trial for the Particular Service Platform has expired");
                    PromptUserForLicenseIfTrialHasExpired();
                    return;
                }

                Logger.Fatal("Your license has expired! You can renew it at https://particular.net/licensing.");
                return;
            }

            if (license.IsTrialLicense)
            {
                var trialStartDate = TrialStartDateStore.GetTrialStartDate();
                var message = $"Trial for the Particular Service Platform has been active since {trialStartDate.ToLocalTime().ToShortDateString()}.";
                Logger.Info(message);
            }

            if (license.UpgradeProtectionExpiration != null)
            {
                Logger.InfoFormat("License upgrade protection expires on: {0}", license.UpgradeProtectionExpiration);
            }
            else
            {
                Logger.InfoFormat("License expires on {0}", license.ExpirationDate);
            }
        }

        void PromptUserForLicenseIfTrialHasExpired()
        {
            if (!(Debugger.IsAttached && SystemInformation.UserInteractive))
            {
                //We only prompt user if user is in debugging mode and we are running in interactive mode
                return;
            }

            bool createdNew;
            using (new Mutex(true, $"NServiceBus-{GitFlowVersion.MajorMinor}", out createdNew))
            {
                if (!createdNew)
                {
                    //Dialog already displaying for this software version by another process, so we just use the already assigned license.
                    return;
                }

                if (license == null || LicenseExpirationChecker.HasLicenseExpired(license))
                {
                    var licenseProvidedByUser = LicenseExpiredFormDisplayer.PromptUserForLicense(license);

                    if (licenseProvidedByUser != null)
                    {
                        license = licenseProvidedByUser;
                    }
                }
            }
        }

        License license;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
    }
}