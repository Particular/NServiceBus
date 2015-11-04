namespace NServiceBus.Features
{
    using System.Collections.Generic;
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
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryTimeoutPersister>(DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}