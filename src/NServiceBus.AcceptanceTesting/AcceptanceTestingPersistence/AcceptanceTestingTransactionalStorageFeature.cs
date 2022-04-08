namespace NServiceBus.AcceptanceTesting
{
    using Features;
    using Persistence;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingTransactionalStorageFeature : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddScoped<ICompletableSynchronizedStorageSession, AcceptanceTestingSynchronizedStorageSession>();
        }
    }
}