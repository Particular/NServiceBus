namespace NServiceBus.Features
{
    using System;
    using Logging;
    using NServiceBus.Licensing;

    class LicenseReminder : Feature
    {
        public LicenseReminder()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var expiredLicense = true;
            try
            {
                LicenseManager.InitializeLicense();

                expiredLicense = LicenseManager.HasLicenseExpired();
            }
            catch (Exception ex)
            {
                //we only log here to prevent licensing issue to abort startup and cause production outages
                Logger.Fatal("Failed to initialize the license", ex);
            }

            context.Container.ConfigureComponent<LicenseBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.LicenseExpired, expiredLicense);

            context.Pipeline.Register("LicenseReminder", typeof(LicenseBehavior), "Reminds users if license has expired");
        }

        static ILog Logger = LogManager.GetLogger<LicenseReminder>();
    }
}