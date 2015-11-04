namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using NServiceBus.InMemory.SubscriptionStorage;

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
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySubscriptionStorage>(DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}