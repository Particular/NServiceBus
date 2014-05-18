namespace NServiceBus.InMemory.SubscriptionStorage
{
    using Features;

    public class InMemorySubscriptionPersistence:Feature
    {
        public InMemorySubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }


        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<InMemorySubscriptionStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}