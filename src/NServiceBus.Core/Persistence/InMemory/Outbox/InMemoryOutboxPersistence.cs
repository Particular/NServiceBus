namespace NServiceBus.InMemory.Outbox
{
    using Features;

    public class InMemoryOutboxPersistence : Feature
    {
        internal InMemoryOutboxPersistence()
        {
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}