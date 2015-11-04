namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Logging;
    using NServiceBus.Licensing;

    class LicenseReminder : Feature
    {
        public LicenseReminder()
        {
            EnableByDefault();

            Defaults(s => s.SetDefault(LicenseTextSettingsKey, null));
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            try
            {
                var licenseManager = new LicenseManager();
                licenseManager.InitializeLicense(context.Settings.Get<string>(LicenseTextSettingsKey));

                context.Container.RegisterSingleton(licenseManager);

                var licenseExpired = licenseManager.HasLicenseExpired();
                if (!licenseExpired)
                {
                    return FeatureStartupTask.None;
                }

                context.Pipeline.Register("LicenseReminder", typeof(AuditInvalidLicenseBehavior), "Audits that the message was processed by an endpoint with an expired license");

                if (Debugger.IsAttached)
                {
                    context.Pipeline.Register("LogErrorOnInvalidLicense", typeof(LogErrorOnInvalidLicenseBehavior), "Logs an error when running in debug mode with an expired license");
                }
            }
            catch (Exception ex)
            {
                //we only log here to prevent licensing issue to abort startup and cause production outages
                Logger.Fatal("Failed to initialize the license", ex);
            }
            return FeatureStartupTask.None;
        }

        static ILog Logger = LogManager.GetLogger<LicenseReminder>();

        public const string LicenseTextSettingsKey = "LicenseText";
    }
}