namespace NServiceBus.InMemory.TimeoutPersister
{
    using Features;

    /// <summary>
    /// Used to configure in memory timeout persistence.
    /// </summary>
    public class InMemoryTimeoutPersistence : Feature
    {
        internal InMemoryTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryTimeoutPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}