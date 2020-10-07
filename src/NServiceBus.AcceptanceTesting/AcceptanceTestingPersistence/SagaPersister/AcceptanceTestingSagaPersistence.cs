namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using System;
    using AcceptanceTesting.AcceptanceTestingPersistence;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using TimeoutPersister;

    /// <summary>
    /// Used to configure in memory saga persistence.
    /// </summary>
    class AcceptanceTestingSagaPersistence : Feature
    {
        internal AcceptanceTestingSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.EnableFeature(typeof(AcceptanceTestingTransactionalStorageFeature)));
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingTimeoutPersister(() => DateTime.UtcNow));
        }
    }
}