namespace NServiceBus.AcceptanceTesting;

using Features;
using Persistence;
using Microsoft.Extensions.DependencyInjection;

class AcceptanceTestingTransactionalStorageFeature : Feature, IFeatureFactory
{
    public AcceptanceTestingTransactionalStorageFeature() => DependsOn<SynchronizedStorage>();

    protected override void Setup(FeatureConfigurationContext context)
        => context.Services.AddScoped<ICompletableSynchronizedStorageSession, AcceptanceTestingSynchronizedStorageSession>();

    static Feature IFeatureFactory.Create() => new AcceptanceTestingTransactionalStorageFeature();
}