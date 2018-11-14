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

        public LicenseManager(Func<DateTime> utcDateProvider)
        {
            this.utcDateProvider = utcDateProvider;
        }

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            result = ActiveLicense.Find("NServiceBus", licenseSources);

            LogFindResults(result);

            var licenseStatus = result.License.GetLicenseStatus();
            LogLicenseStatus(licenseStatus, Logger);

            if (licenseStatus == LicenseStatus.InvalidDueToExpiredTrial)
            {
                OpenTrialExtensionPage();
            }
        }

        internal void LogLicenseStatus(LicenseStatus licenseStatus, ILog logger)
        {
            switch (licenseStatus)
            {
                case LicenseStatus.Valid:
                    break;
                case LicenseStatus.ValidWithExpiredUpgradeProtection:
                    logger.Warn("Upgrade protection expired. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.");
                    break;
                case LicenseStatus.ValidWithExpiringTrial:
                    logger.Warn("Trial license expiring soon. Please extend your trial or purchase a license to continue using the Particular Service Platform.");
                    break;
                case LicenseStatus.ValidWithExpiringSubscription:
                    logger.Warn("Platform license expiring soon. Please extend your license to continue using the Particular Service Platform.");
                    break;
                case LicenseStatus.ValidWithExpiringUpgradeProtection:
                    logger.Warn("Upgrade protection expiring soon. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.");
                    break;
                case LicenseStatus.InvalidDueToExpiredTrial:
                    logger.Error("Trial license expired. Please extend your trial or purchase a license to continue using the Particular Service Platform.");
                    break;
                case LicenseStatus.InvalidDueToExpiredSubscription:
                    logger.Error("Platform license expired. Please extend your license to continue using the Particular Service Platform.");
                    break;
                case LicenseStatus.InvalidDueToExpiredUpgradeProtection:
                    logger.Error("Upgrade protection expired. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.");
                    break;
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
        readonly Func<DateTime> utcDateProvider;

        static readonly ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool DebugLoggingEnabled = Logger.IsDebugEnabled;
        static readonly TimeSpan ExpirationWarningThreshold = TimeSpan.FromDays(10);
    }
}