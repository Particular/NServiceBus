namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using Features;

    /// <summary>
    /// Used to configure in memory subscription persistence.
    /// </summary>
    public class AcceptanceTestingSubscriptionPersistence : Feature
    {
        internal AcceptanceTestingSubscriptionPersistence()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<AcceptanceTestingSubscriptionStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}