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
            try
            {
                LicenseManager.InitializeLicense();

                if (LicenseManager.HasLicenseExpired())
                {
                    context.Pipeline.Register<NotifyOnInvalidLicenseBehavior.Registration>();
                }
            }
            catch (Exception ex)
            {
                //we only log here to prevent licensing issue to abort startup and cause production outages
                Logger.Fatal("Failed to initialize the license", ex);
            }

            
        }

        static ILog Logger = LogManager.GetLogger<LicenseReminder>();
    }
}