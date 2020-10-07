namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using System;
    using AcceptanceTesting.AcceptanceTestingPersistence;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using TimeoutPersister;

    class AcceptanceTestingSagaPersistence : Feature
    {
        internal AcceptanceTestingSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.EnableFeature(typeof(AcceptanceTestingTransactionalStorageFeature)));
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingTimeoutPersister(() => DateTime.UtcNow));
        }
    }
}