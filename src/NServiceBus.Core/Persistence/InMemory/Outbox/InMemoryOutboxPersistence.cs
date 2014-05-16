namespace NServiceBus.InMemory.Outbox
{
    using Features;

    public class InMemoryOutboxPersistence:Feature
    {
        public InMemoryOutboxPersistence()
        {
            DependsOn<Outbox>();
        }

        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}