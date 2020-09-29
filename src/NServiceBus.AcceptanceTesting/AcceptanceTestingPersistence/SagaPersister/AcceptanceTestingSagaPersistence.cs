namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using AcceptanceTesting.AcceptanceTestingPersistence;
    using Features;

    /// <summary>
    /// Used to configure in memory saga persistence.
    /// </summary>
    public class AcceptanceTestingSagaPersistence : Feature
    {
        internal AcceptanceTestingSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.EnableFeature(typeof(AcceptanceTestingTransactionalStorageFeature)));
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<AcceptanceTestingSagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}