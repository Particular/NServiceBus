namespace NServiceBus.Features
{
    using NServiceBus.InMemory.TimeoutPersister;

    /// <summary>
    /// Used to configure in memory timeout persistence.
    /// </summary>
    public class InMemoryTimeoutPersistence : Feature
    {
        internal InMemoryTimeoutPersistence()
        {
            DependsOn<TimeoutManager>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryTimeoutPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}