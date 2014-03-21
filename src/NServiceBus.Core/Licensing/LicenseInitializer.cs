namespace NServiceBus.Licensing
{
    using Pipeline;
    using Pipeline.Contexts;

    class LicenseInitializer : PipelineOverride, INeedInitialization
    {
        public void Init()
        {
            LicenseManager.InitializeLicense();

            Configure.Component<LicenseBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.LicenseExpired, LicenseManager.HasLicenseExpired());

        }

        public override void Override(BehaviorList<ReceivePhysicalMessageContext> behaviorList)
        {
            behaviorList.Add<LicenseBehavior>();
        }
    }
}