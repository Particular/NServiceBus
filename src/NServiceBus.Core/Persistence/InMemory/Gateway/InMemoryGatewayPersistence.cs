namespace NServiceBus.InMemory.Gateway
{
    using Features;
    using NServiceBus.Gateway.Deduplication;
    using NServiceBus.Gateway.Persistence;

    public class InMemoryGatewayPersistence:Feature
    {
        public InMemoryGatewayPersistence()
        {
            DependsOn<Gateway>();
        }

        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryGatewayDeduplication>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<InMemoryGatewayPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}