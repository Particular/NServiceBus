namespace NServiceBus
{
    using System;
    using System.Diagnostics;
#if NETSTANDARD2_0
    using System.Runtime.InteropServices;
#endif
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
                    Logger.Fatal("Your license has expired! To renew your license, visit: https://particular.net/licensing");
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

#if REGISTRYLICENSESOURCE
            if (result.Location.StartsWith("HKEY_"))
            {
                Logger.Warn("Reading license information from the registry has been deprecated and will be removed in version 8.0. See the documentation for more details.");
            }
#endif

#if APPCONFIGLICENSESOURCE
            if (result.Location.StartsWith("app config"))
            {
                Logger.Warn("Reading license information from the app config file has been deprecated and will be removed in version 8.0. See the documentation for more details.");
            }
#endif
        }

        void OpenTrialExtensionPage()
        {
            var version = GitFlowVersion.MajorMinorPatch;
            var extendedTrial = result.License.IsExtendedTrial ? "1" : "0";
            var platform = GetPlatformCode();
            var url = $"https://particular.net/license/nservicebus?v={version}&t={extendedTrial}&p={platform}";

            if (!(Debugger.IsAttached && Environment.UserInteractive))
            {
                Logger.WarnFormat("To extend your trial license, visit: {0}", url);

                return;
            }

            using (var mutex = new Mutex(true, @"Global\NServiceBusLicensing", out var acquired))
            {
                if (acquired)
                {
                    try
                    {
                        Logger.WarnFormat("Opening browser to: {0}", url);

                        var opened = Browser.TryOpen(url);

                        if (!opened)
                        {
                            Logger.WarnFormat("Unable to open browser. To extend your trial license, visit: {0}", url);
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

        string GetPlatformCode()
        {
#if NET452
            return "windows";
#elif NETSTANDARD2_0
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "windows";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macos";
            }
            return "unknown";
#endif
        }

        ActiveLicenseFindResult result;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool debugLoggingEnabled = Logger.IsDebugEnabled;
    }
}
