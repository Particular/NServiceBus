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

        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryGatewayDeduplication>(DependencyLifecycle.SingleInstance);
        }
    }
}