namespace NServiceBus.AcceptanceTesting
{
    using Features;
    using Persistence;
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