namespace NServiceBus.Licensing
{
    using System;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;

    class LicenseInitializer : PipelineOverride, INeedInitialization
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

        }

        public override void Override(BehaviorList<IncomingContext> behaviorList)
        {
            behaviorList.Add<LicenseBehavior>();
        }

        static ILog Logger = LogManager.GetLogger(typeof(LicenseInitializer));
    }
}