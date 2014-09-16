namespace NServiceBus.Features
{
    using NServiceBus.Gateway.Deduplication;

    /// <summary>
    /// In-memory Gateway
    /// </summary>
    public class InMemoryGatewayPersistence : Feature
    {
        internal InMemoryGatewayPersistence()
        {
            DependsOn("Gateway");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryGatewayDeduplication>(DependencyLifecycle.SingleInstance);
        }
    }
}