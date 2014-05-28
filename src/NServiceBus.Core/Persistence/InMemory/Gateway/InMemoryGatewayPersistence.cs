namespace NServiceBus.InMemory.Gateway
{
    using Features;
    using NServiceBus.Gateway.Deduplication;

    public class InMemoryGatewayPersistence:Feature
    {
        public InMemoryGatewayPersistence()
        {
            DependsOn<Gateway>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryGatewayDeduplication>(DependencyLifecycle.SingleInstance);
        }
    }
}