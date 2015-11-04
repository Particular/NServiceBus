namespace NServiceBus.Features
{
    using System.Collections.Generic;

    /// <summary>
    /// Used to configure in memory saga persistence.
    /// </summary>
    public class InMemorySagaPersistence : Feature
    {
        internal InMemorySagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.EnableFeature(typeof(InMemoryTransactionalStorageFeature)));
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySagaPersister>(DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}