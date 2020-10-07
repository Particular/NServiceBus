namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingTransactionalStorageFeature : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingSynchronizedStorage());
            context.Services.AddSingleton(_ => new AcceptanceTestingTransactionalSynchronizedStorageAdapter());
        }
    }
}