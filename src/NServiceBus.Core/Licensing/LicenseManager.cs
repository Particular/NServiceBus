namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Particular.Licensing;
#if NETSTANDARD
    using System.Runtime.InteropServices;

#endif

    class LicenseManager
    {
        internal bool HasLicenseExpired => result?.HasExpired ?? true;

        public LicenseManager(Func<DateTime> utcDateTimeProvider)
        {
            this.utcDateTimeProvider = utcDateTimeProvider;
        }

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            result = ActiveLicense.Find("NServiceBus", licenseSources);

            LogFindResults(result);
            if (result.HasExpired)
            {
                LogExpiredLicenseError(result.License, Logger);
            }
            else
            {
                LogExpiringLicenseWarning(result.License, Logger);
            }

            if (result.HasExpired && result.License.IsTrialLicense)
            {
                OpenTrialExtensionPage();
            }
        }

        internal void LogExpiredLicenseError(License activeLicense, ILog logger)
        {
            if (activeLicense.UpgradeProtectionExpiration.HasValue)
            {
                logger.Error("Upgrade protection expired. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.");
            }
            else if (activeLicense.IsTrialLicense)
            {
                logger.Error("Trial license expired. Please extend your trial or purchase a license to continue using the Particular Service Platform.");
            }
            else
            {
                logger.Error("Platform license expired. Please extend your license to continue using the Particular Service Platform.");
            }
        }

        internal void LogExpiringLicenseWarning(License activeLicense, ILog logger)
        {
            if (activeLicense.UpgradeProtectionExpiration.HasValue)
            {
                if (activeLicense.UpgradeProtectionExpiration.Value.Subtract(ExpirationWarningThreshold) <= utcDateTimeProvider().Date)
                {
                    logger.Warn("Upgrade protection expiring soon. Please extend your upgrade protection so that we can continue to provide you with support and new versions of the Particular Service Platform.");
                }
            }
            else if (activeLicense.ExpirationDate?.Subtract(ExpirationWarningThreshold) <= utcDateTimeProvider().Date)
            {
                if (activeLicense.IsTrialLicense)
                {
                    logger.Warn("Trial license expiring soon. Please extend your trial or purchase a license to continue using the Particular Service Platform.");
                }
                else
                {
                    logger.Warn("Platform license expiring soon. Please extend your license to continue using the Particular Service Platform.");
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
        readonly Func<DateTime> utcDateTimeProvider;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool debugLoggingEnabled = Logger.IsDebugEnabled;
        static readonly TimeSpan ExpirationWarningThreshold = TimeSpan.FromDays(3);
    }
}