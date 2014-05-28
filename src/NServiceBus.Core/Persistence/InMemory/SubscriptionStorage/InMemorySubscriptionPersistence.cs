namespace NServiceBus.InMemory.SubscriptionStorage
{
    using Features;

    public class InMemorySubscriptionPersistence:Feature
    {
        public InMemorySubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }


        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySubscriptionStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}