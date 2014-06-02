namespace NServiceBus.InMemory.Gateway
{
    using Features;
    using NServiceBus.Gateway.Deduplication;

    /// <summary>
    /// In-memory Gateway
    /// </summary>
    public class InMemoryGatewayPersistence:Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="InMemoryGatewayPersistence"/>.
        /// </summary>
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