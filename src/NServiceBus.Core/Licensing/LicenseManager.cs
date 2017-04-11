namespace NServiceBus
{
    using System.Diagnostics;
    using System.Text;
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

            var report = new StringBuilder();
            report.AppendLine("Looking for license in the following locations:");

            foreach (var item in result.Report)
            {
                report.AppendLine(item);
            }

            Logger.Info(report.ToString());

            license = result.License;

            if (result.HasExpired)
            {
                if (license.IsTrialLicense)
                {
                    Logger.WarnFormat("Trial for the Particular Service Platform has expired.");
                    PromptUserForLicenseIfTrialHasExpired();
                    return;
                }

                Logger.Fatal("Your license has expired! You can renew it at https://particular.net/licensing.");
                return;
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