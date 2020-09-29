namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using Features;

    class AcceptanceTestingTransactionalStorageFeature : Feature
    {
        /// <summary>
        /// Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<AcceptanceTestingSynchronizedStorage>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<AcceptanceTestingTransactionalSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}