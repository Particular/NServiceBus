namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Text;
#if NET452
    using System.Threading;
#endif
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

            LogFindResults(result);

            if (result.HasExpired)
            {
                if (license.IsTrialLicense)
                {
                    Logger.WarnFormat("Trial for the Particular Service Platform has expired.");
                    PromptUserForLicenseIfTrialHasExpired();
                }
                else
                {
                    Logger.Fatal("Your license has expired! You can renew it at https://particular.net/licensing.");
                }
            }
        }

        static void LogFindResults(ActiveLicenseFindResult result)
        {
            var report = new StringBuilder();

            if (debugLoggingEnabled)
            {
                report.AppendLine("Looking for license in the following locations:");

                foreach (var item in result.Report)
                {
                    report.AppendLine(item);
                }

                Logger.Debug(report.ToString());
            }
            else
            {
                foreach (var item in result.SelectedLicenseReport)
                {
                    report.AppendLine(item);
                }

                Logger.Info(report.ToString());
            }
        }

        void PromptUserForLicenseIfTrialHasExpired()
        {
            if (!(Debugger.IsAttached && Environment.UserInteractive))
            {
                //We only prompt user if user is in debugging mode and we are running in interactive mode
                return;
            }

#if NET452
            var licenseProvidedByUser = RequestLicenseFromPopupDialog();
#endif
#if NETCOREAPP2_0
            var licenseProvidedByUser = ConsoleLicensePrompt.RequestLicenseFromConsole();
#endif

            if (licenseProvidedByUser != null)
            {
                license = licenseProvidedByUser;
            }
        }

#if NET452
        License RequestLicenseFromPopupDialog()
        {
            bool createdNew;
            using (new Mutex(true, $"NServiceBus-{GitFlowVersion.MajorMinor}", out createdNew))
            {
                if (!createdNew)
                {
                    //Dialog already displaying for this software version by another process, so we just use the already assigned license.
                    return null;
                }

                if (license == null || LicenseExpirationChecker.HasLicenseExpired(license))
                {
                    return LicenseExpiredFormDisplayer.PromptUserForLicense(license);
                }

                return null;
            }
        }
#endif

        License license;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool debugLoggingEnabled = Logger.IsDebugEnabled;
    }
}