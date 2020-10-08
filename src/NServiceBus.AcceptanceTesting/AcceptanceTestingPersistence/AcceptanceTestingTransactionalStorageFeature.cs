using NServiceBus.Persistence;

namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingTransactionalStorageFeature : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton<ISynchronizedStorage, AcceptanceTestingSynchronizedStorage>();
            context.Services.AddSingleton<ISynchronizedStorageAdapter, AcceptanceTestingTransactionalSynchronizedStorageAdapter>();
        }
    }
}