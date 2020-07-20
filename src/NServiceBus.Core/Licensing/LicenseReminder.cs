namespace NServiceBus.Features
{
    using System;
    using System.Diagnostics;
    using Logging;

    class LicenseReminder : Feature
    {
        public LicenseReminder()
        {
            EnableByDefault();

            Defaults(s => s.SetDefault(LicenseTextSettingsKey, null));
            Defaults(s => s.SetDefault(LicenseFilePathSettingsKey, null));
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            try
            {
                var licenseManager = new LicenseManager();
                licenseManager.InitializeLicense(context.Settings.Get<string>(LicenseTextSettingsKey), context.Settings.Get<string>(LicenseFilePathSettingsKey));

                context.Settings.AddStartupDiagnosticsSection("Licensing", GenerateLicenseDiagnostics(licenseManager));
                
                if (!licenseManager.HasLicenseExpired)
                {
                    return;
                }

                context.Pipeline.Register("LicenseReminder", new AuditInvalidLicenseBehavior(), "Audits that the message was processed by an endpoint with an expired license");

                if (Debugger.IsAttached)
                {
                    context.Pipeline.Register("LogErrorOnInvalidLicense", new LogErrorOnInvalidLicenseBehavior(), "Logs an error when running in debug mode with an expired license");
                }
            }
            catch (Exception ex)
            {
                //we only log here to prevent licensing issue to abort startup and cause production outages
                Logger.Fatal("Failed to initialize the license", ex);
            }
        }

        static object GenerateLicenseDiagnostics(LicenseManager licenseManager)
        {
            return new
            {
                LicenseLocation = licenseManager.result.Location,
                RegisteredTo = licenseManager.result.License.RegisteredTo,
                LicenseStatus = licenseManager.result.License.GetLicenseStatus(),
                LicenseType = licenseManager.result.License.LicenseType,
                Edition = licenseManager.result.License.Edition,
                ValidApplications = string.Join(",", licenseManager.result.License.ValidApplications),
                CommercialLicense = licenseManager.result.License.IsCommercialLicense,
                IsExpired = licenseManager.HasLicenseExpired,
                ExpirationDate = licenseManager.result.License.ExpirationDate,
                UpgradeProtectionExpirationDate = licenseManager.result.License.UpgradeProtectionExpiration
            };
        }

        public const string LicenseTextSettingsKey = "LicenseText";
        public const string LicenseFilePathSettingsKey = "LicenseFilePath";

        static ILog Logger = LogManager.GetLogger<LicenseReminder>();
    }
}