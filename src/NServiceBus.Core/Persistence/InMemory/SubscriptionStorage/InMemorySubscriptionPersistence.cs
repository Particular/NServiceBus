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


        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySubscriptionStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}