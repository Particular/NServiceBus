namespace NServiceBus
{
    using Features;

    class InMemoryTransactionalStorageFeature : Feature
    {
        /// <summary>
        /// Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySynchronizedStorage>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<InMemoryTransactionalSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}