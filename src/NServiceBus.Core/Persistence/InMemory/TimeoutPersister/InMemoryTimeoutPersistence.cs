namespace NServiceBus.InMemory.TimeoutPersister
{
    using Features;

    public class InMemoryTimeoutPersistence:Feature
    {
        public InMemoryTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryTimeoutPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}