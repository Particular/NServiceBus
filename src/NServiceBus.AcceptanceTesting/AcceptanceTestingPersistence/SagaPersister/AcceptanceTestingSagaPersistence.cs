using NServiceBus.Sagas;

namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using AcceptanceTesting.AcceptanceTestingPersistence;
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingSagaPersistence : Feature
    {
        public AcceptanceTestingSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.EnableFeature(typeof(AcceptanceTestingTransactionalStorageFeature)));
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton<ISagaPersister, AcceptanceTestingSagaPersister>();
        }
    }
}