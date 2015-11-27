namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using NServiceBus.Gateway.Deduplication;

    /// <summary>
    /// In-memory Gateway.
    /// </summary>
    public class InMemoryGatewayPersistence : Feature
    {
        internal InMemoryGatewayPersistence()
        {
            DependsOn("Gateway");
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryGatewayDeduplication>(DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}