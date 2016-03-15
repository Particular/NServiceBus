namespace NServiceBus.Features
{
    using System;
    using Persistence;

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
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.GatewayDeduplication>(context.Settings))
            {
                throw new Exception("Selected persister doesn't have support for subscription storage. Select another storage or disable the saga feature using endpointConfiguration.DisableFeature<Gateway>()");
            }

            context.Container.ConfigureComponent<InMemoryGatewayDeduplication>(DependencyLifecycle.SingleInstance);
        }
    }
}