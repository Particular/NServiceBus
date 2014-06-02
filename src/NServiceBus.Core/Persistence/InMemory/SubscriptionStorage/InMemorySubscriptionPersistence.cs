namespace NServiceBus.InMemory.SubscriptionStorage
{
    using Features;
    /// <summary>
    /// Used to configure in memory subscription persistence.
    /// </summary>
    public class InMemorySubscriptionPersistence : Feature
    {
        internal InMemorySubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }


        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySubscriptionStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}