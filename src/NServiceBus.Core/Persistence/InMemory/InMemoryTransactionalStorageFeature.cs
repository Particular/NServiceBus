namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Features;

    class InMemoryTransactionalStorageFeature : Feature
    {
        /// <summary>
        ///     Called when the features is activated.
        /// </summary>
        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemorySynchronizedStorage>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<InMemoryTransactionalSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);

            return FeatureStartupTask.None;
        }
    }
}