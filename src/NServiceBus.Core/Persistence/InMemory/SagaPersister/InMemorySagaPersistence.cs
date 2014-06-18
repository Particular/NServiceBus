namespace NServiceBus.InMemory.SagaPersister
{
    using Features;
    /// <summary>
    /// Used to configure in memory saga persistence.
    /// </summary>
    public class InMemorySagaPersistence : Feature
    {
        internal InMemorySagaPersistence()
        {
            DependsOn<Sagas>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}