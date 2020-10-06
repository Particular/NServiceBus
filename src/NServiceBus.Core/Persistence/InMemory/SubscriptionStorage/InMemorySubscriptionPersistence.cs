namespace NServiceBus.Features
{
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
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySubscriptionStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}