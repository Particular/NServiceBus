namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Particular.Licensing;

    class LicenseManager
    {
        public bool HasLicenseExpired => result?.License.HasExpired() ?? true;

        public void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            result = ActiveLicense.Find("NServiceBus", licenseSources);
            var developerLicenseUrl = CreateDeveloperLicenseUrl();

            LogFindResults(result);

            var licenseStatus = result.License.GetLicenseStatus();
            LogLicenseStatus(licenseStatus, Logger, result.License, developerLicenseUrl);

            if (licenseStatus == LicenseStatus.InvalidDueToExpiredTrial)
            {
                OpenDeveloperLicensePage(developerLicenseUrl);
            }
        }

        public void LogLicenseStatus(LicenseStatus licenseStatus, ILog logger, License license, string developerLicenseUrl)
        {
            var whenLicenseExpiresPhrase = GetRemainingDaysString(license.GetDaysUntilLicenseExpires());
            var whenUpgradeProtectedExpiresPhrase = GetRemainingDaysString(license.GetDaysUntilUpgradeProtectionExpires());

            switch (licenseStatus)
            {
                case LicenseStatus.Valid:
                    break;
                case LicenseStatus.ValidWithExpiredUpgradeProtection:
                    logger.Warn("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, contact us to renew your license: contact@particular.net");
                    break;
                case LicenseStatus.ValidWithExpiringTrial:
                    if (license.IsExtendedTrial)
                    {
                        logger.Warn($"Development license expiring {whenLicenseExpiresPhrase}. If you’re still in development, renew your license for free at {developerLicenseUrl} otherwise email contact@particular.net");
                    }
                    else
                    {
                        logger.Warn($"Trial license expiring {whenLicenseExpiresPhrase}. Get your free development license at {developerLicenseUrl}");
                    }
                    break;
                case LicenseStatus.ValidWithExpiringSubscription:
                    logger.Warn($"License expiring {whenLicenseExpiresPhrase}. Contact us to renew your license: contact@particular.net");
                    break;
                case LicenseStatus.ValidWithExpiringUpgradeProtection:
                    logger.Warn($"Upgrade protection expiring {whenUpgradeProtectedExpiresPhrase}. Contact us to renew your license: contact@particular.net");
                    break;
                case LicenseStatus.InvalidDueToExpiredTrial:
                    if (license.IsExtendedTrial)
                    {
                        logger.Error($"Development license expired. If you’re still in development, renew your license for free at {developerLicenseUrl} otherwise email contact@particular.net");
                    }
                    else
                    {
                        logger.Error($"Trial license expired. Get your free development license at {developerLicenseUrl}");
                    }
                    break;
                case LicenseStatus.InvalidDueToExpiredSubscription:
                    logger.Error("License expired. Contact us to renew your license: contact@particular.net");
                    break;
                case LicenseStatus.InvalidDueToExpiredUpgradeProtection:
                    logger.Error("Upgrade protection expired. In order for us to continue to provide you with support and new versions of the Particular Service Platform, contact us to renew your license: contact@particular.net");
                    break;
                default:
                    break;
            }

            static string GetRemainingDaysString(int? remainingDays) => remainingDays switch
            {
                null => "soon",
                0 => "today",
                1 => "in 1 day",
                _ => $"in {remainingDays} days"
            };
        }

        static void LogFindResults(ActiveLicenseFindResult result)
        {
            if (Logger.IsDebugEnabled)
            {
                var report = new StringBuilder("Looking for license in the following locations:");

                foreach (var item in result.Report)
                {
                    report.NewLine(item);
                }

                Logger.Debug(report.ToString());
            }

            Logger.Info(string.Join(Environment.NewLine, result.SelectedLicenseReport));
        }

        void OpenDeveloperLicensePage(string developerLicenseUrl)
        {
            if (!(Debugger.IsAttached && Environment.UserInteractive))
            {
                Logger.WarnFormat("To extend your trial license, visit: {0}", developerLicenseUrl);

                return;
            }

            using (var mutex = new Mutex(true, @"Global\NServiceBusLicensing", out var acquired))
            {
                if (acquired)
                {
                    try
                    {
                        Logger.WarnFormat("Opening browser to: {0}", developerLicenseUrl);

                        var opened = Browser.TryOpen(developerLicenseUrl);

                        if (!opened)
                        {
                            Logger.WarnFormat("Unable to open browser. To extend your trial license, visit: {0}", developerLicenseUrl);
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

        string CreateDeveloperLicenseUrl()
        {
            var version = VersionInformation.MajorMinorPatch;
            var isRenewal = result.License.IsExtendedTrial ? "1" : "0";
            var platform = GetPlatformCode();
            var frameworkVersion = GetFrameworkVersion();
            return $"https://particular.net/license/nservicebus?v={version}&t={isRenewal}&p={platform}&f={frameworkVersion}";
        }

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

        string GetFrameworkVersion()
        {
            var match = Regex.Match(RuntimeInformation.FrameworkDescription, @"\d+");
            return match.Success ? match.Value : "0";
        }

        internal ActiveLicenseFindResult result;

        static readonly ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
    }
}