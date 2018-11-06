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
                var licenseManager = new LicenseManager(() => DateTime.UtcNow);
                licenseManager.InitializeLicense(context.Settings.Get<string>(LicenseTextSettingsKey), context.Settings.Get<string>(LicenseFilePathSettingsKey));

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

        public const string LicenseTextSettingsKey = "LicenseText";
        public const string LicenseFilePathSettingsKey = "LicenseFilePath";

        static ILog Logger = LogManager.GetLogger<LicenseReminder>();
    }
}