namespace NServiceBus.Licensing
{
    using System;
    using Logging;

    class LicenseInitializer : INeedInitialization
    {
        public void Init()
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
            
            Configure.Component<LicenseBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.LicenseExpired, expiredLicense);

            Configure.Pipeline.Register("LicenseReminder", typeof(LicenseBehavior), "Reminds users if license has expired");
        }

        static ILog Logger = LogManager.GetLogger(typeof(LicenseInitializer));
    }
}