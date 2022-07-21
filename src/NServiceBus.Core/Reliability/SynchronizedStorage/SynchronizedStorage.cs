namespace NServiceBus.Features
{
    using Persistence;

    /// <summary>
    /// Configures the synchronized storage.
    /// </summary>
    public class SynchronizedStorage : Feature
    {
        internal SynchronizedStorage()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SynchronizedStorageSession>(
                builder => builder.Build<CompletableSynchronizedStorageSession>(),
                DependencyLifecycle.InstancePerUnitOfWork);
        }
    }
}