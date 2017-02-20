namespace NServiceBus.Features
{
    using NServiceBus.Sagas;

    /// <summary>
    /// Used to configure development saga persistence.
    /// </summary>
    public class DevelopmentSagaPersistence : Feature
    {
        internal DevelopmentSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.Set<ISagaIdGenerator>(new DevelopmentSagaIdGenerator()));
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b => new DevelopmentSagaPersister(@"c:\dev\storage"), DependencyLifecycle.SingleInstance);
        }
    }
}