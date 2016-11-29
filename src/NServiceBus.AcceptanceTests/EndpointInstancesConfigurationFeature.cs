namespace NServiceBus.AcceptanceTests
{
    using Features;

    class EndpointInstancesConfigurationFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Routing.EndpointInstances.AddOrReplaceInstances("testing", context.Settings.GetRegisteredEndpointInstances());
        }
    }
}