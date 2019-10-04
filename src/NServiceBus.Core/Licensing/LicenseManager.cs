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
        internal bool HasLicenseExpired => result?.License.HasExpired() ?? true;

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            result = ActiveLicense.Find("NServiceBus", licenseSources);

            LogFindResults(result);

            var licenseStatus = result.License.GetLicenseStatus();
            LogLicenseStatus(licenseStatus, Logger, result.License);

            if (licenseStatus == LicenseStatus.InvalidDueToExpiredTrial)
            {
                OpenTrialExtensionPage();
            }
        }

        internal void LogLicenseStatus(LicenseStatus licenseStatus, ILog logger, License license)
        {
            switch (licenseStatus)
            {
                case LicenseStatus.Valid:
                    break;
                case LicenseStatus.ValidWithExpiredUpgradeProtection:
                    logger.Warn("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expired");
                    break;
                case LicenseStatus.ValidWithExpiringTrial:
                    logger.WarnFormat("Trial license expiring {0}. To continue using the Particular Service Platform, please extend your trial or purchase a license by visiting http://go.particular.net/trial-expiring", GetRemainingDaysString(license.GetDaysUntilLicenseExpires()));
                    break;
                case LicenseStatus.ValidWithExpiringSubscription:
                    logger.WarnFormat("Platform license expiring {0}. To continue using the Particular Service Platform, please extend your license by visiting http://go.particular.net/license-expiring", GetRemainingDaysString(license.GetDaysUntilLicenseExpires()));
                    break;
                case LicenseStatus.ValidWithExpiringUpgradeProtection:
                    logger.WarnFormat("Upgrade protection expiring {0}. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expiring", GetRemainingDaysString(license.GetDaysUntilUpgradeProtectionExpires()));
                    break;
                case LicenseStatus.InvalidDueToExpiredTrial:
                    logger.Error("Trial license expired. To continue using the Particular Service Platform, please extend your trial or purchase a license by visiting http://go.particular.net/trial-expired");
                    break;
                case LicenseStatus.InvalidDueToExpiredSubscription:
                    logger.Error("Platform license expired. To continue using the Particular Service Platform, please extend your license by visiting http://go.particular.net/license-expired");
                    break;
                case LicenseStatus.InvalidDueToExpiredUpgradeProtection:
                    logger.Error("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, please extend your upgrade protection by visiting http://go.particular.net/upgrade-protection-expired");
                    break;
            }

            string GetRemainingDaysString(int? remainingDays)
            {
                switch (remainingDays)
                {
                    case null:
                        return "soon";
                    case 0:
                        return "today";
                    case 1:
                        return "in 1 day";
                    default:
                        return $"in {remainingDays} days";
                }
            }
        }

        static void LogFindResults(ActiveLicenseFindResult result)
        {
            var report = new StringBuilder();

            if (DebugLoggingEnabled)
            {
                report.AppendLine("Looking for license in the following locations:");

                foreach (var item in result.Report)
                {
                    report.AppendLine(item);
                }

                Logger.Debug(report.ToString().TrimEnd('\r','\n'));
            }
            else
            {
                foreach (var item in result.SelectedLicenseReport)
                {
                    report.AppendLine(item);
                }

                Logger.Info(report.ToString().TrimEnd('\r', '\n'));
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
            var version = GitVersionInformation.MajorMinorPatch;
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

        static readonly ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool DebugLoggingEnabled = Logger.IsDebugEnabled;
    }
}