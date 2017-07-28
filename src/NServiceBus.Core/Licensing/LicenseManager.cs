namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Particular.Licensing;

    class LicenseManager
    {
        internal bool HasLicenseExpired => result?.HasExpired ?? true;

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            result = ActiveLicense.Find("NServiceBus", licenseSources);

            LogFindResults(result);

            if (result.HasExpired)
            {
                if (result.License.IsTrialLicense)
                {
                    Logger.Warn("Trial for the Particular Service Platform has expired.");
                    OpenTrialExtensionPage();
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

        void OpenTrialExtensionPage()
        {
            string url;

            if (result.License.IsExtendedTrial)
            {
                url = "https://particular.net/extend-your-trial-45";
            }
            else
            {
                url = "https://particular.net/extend-nservicebus-trial";
            }

            if (!(Debugger.IsAttached && Environment.UserInteractive))
            {
                Logger.WarnFormat("Go to '{0}' to extend your trial license", url);

                return;
            }

            using (var mutex = new Mutex(true, @"Global\NServiceBusLicensing", out var acquired))
            {
                if (acquired)
                {
                    try
                    {
                        Logger.WarnFormat("Opening browser to '{0}'", url);

                        var opened = Browser.Open(url);

                        if (!opened)
                        {
                            Logger.WarnFormat("Unable to open '{0}'. Please enter the url manually into a browser.", url);
                        }

                        Task.Delay(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
                else
                {
                    Task.Delay(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                }
            }
        }

        ActiveLicenseFindResult result;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool debugLoggingEnabled = Logger.IsDebugEnabled;
    }
}