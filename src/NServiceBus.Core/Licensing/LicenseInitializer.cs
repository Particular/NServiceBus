namespace NServiceBus.Licensing
{
    using System;
    using Logging;

    class LicenseInitializer : INeedInitialization
    {
        public void Init(Configure config)
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
                Logger.Fatal("Failed to initialize the license",ex);
            }
            
            config.Configurer.ConfigureComponent<LicenseBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.LicenseExpired, expiredLicense);

            config.Pipeline.Register("LicenseReminder", typeof(LicenseBehavior), "Reminds users if license has expired");
        }

        static ILog Logger = LogManager.GetLogger<LicenseInitializer>();
    }
}