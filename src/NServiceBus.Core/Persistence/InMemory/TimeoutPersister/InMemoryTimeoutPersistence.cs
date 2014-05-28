namespace NServiceBus.InMemory.TimeoutPersister
{
    using Features;

    public class InMemoryTimeoutPersistence:Feature
    {
        public InMemoryTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryTimeoutPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}