namespace NServiceBus.AcceptanceTesting;

using Features;
using Sagas;
using Microsoft.Extensions.DependencyInjection;

class AcceptanceTestingSagaPersistence : Feature, IFeatureFactory
{
    public AcceptanceTestingSagaPersistence()
    {
        DependsOn<Sagas>();
        DependsOn<AcceptanceTestingTransactionalStorageFeature>();

        Enable<AcceptanceTestingTransactionalStorageFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context) => context.Services.AddSingleton<ISagaPersister, AcceptanceTestingSagaPersister>();

    static Feature IFeatureFactory.Create() => new AcceptanceTestingSagaPersistence();
}