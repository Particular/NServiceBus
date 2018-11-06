namespace NServiceBus
{
    using System;
    using System.Diagnostics;
#if NETSTANDARD
    using System.Runtime.InteropServices;
#endif
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Particular.Licensing;

    class LicenseManager
    {
        internal Func<DateTime> todayUtc = () => DateTime.Today;
        internal bool HasLicenseExpired => result?.HasExpired ?? true;

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            result = ActiveLicense.Find("NServiceBus", licenseSources);

            LogFindResults(result);
            LogLicenseWarnings(result.License, Logger, result.HasExpired);

            if (result.HasExpired && result.License.IsTrialLicense)
            {
                OpenTrialExtensionPage();
            }
        }

        internal void LogLicenseWarnings(License activeLicense, ILog logger, bool isExpired)
        {
            if (activeLicense.IsTrialLicense)
            {
                if (isExpired)
                {
                    logger.Error("Please extend your trial or purchase a license to continue using the Particular Service Platform.");
                }
                else if (activeLicense.ExpirationDate?.AddDays(-3) <= todayUtc())
                {
                    logger.Warn("Please extend your trial or purchase a license to continue using the Particular Service Platform.");
                }
            }
            else
            {
                if (activeLicense.UpgradeProtectionExpiration.HasValue)
                {
                    if (isExpired)
                    {
                        logger.Error("Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.");
                    }
                    else if (activeLicense.UpgradeProtectionExpiration.Value.AddDays(-3) <= todayUtc())
                    {
                        logger.Warn("Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.");
                    }
                }
                else
                {
                    if (isExpired)
                    {
                        logger.Error("Please extend your license to continue using the Particular Service Platform.");
                    }
                    else if (activeLicense.ExpirationDate?.AddDays(-3) <= todayUtc())
                    {
                        logger.Warn("Please extend your license to continue using the Particular Service Platform.");
                    }
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

#if NETSTANDARD
        string GetPlatformCode()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
        }
#else
        string GetPlatformCode() => "windows";
#endif

        ActiveLicenseFindResult result;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool debugLoggingEnabled = Logger.IsDebugEnabled;
    }
}
