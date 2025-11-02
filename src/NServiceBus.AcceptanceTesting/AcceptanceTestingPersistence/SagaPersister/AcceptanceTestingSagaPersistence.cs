namespace NServiceBus.AcceptanceTesting;

using Features;
using Sagas;
using Microsoft.Extensions.DependencyInjection;

class AcceptanceTestingSagaPersistence : Feature
{
    public AcceptanceTestingSagaPersistence()
    {
        DependsOn<Sagas>();
        DependsOn<AcceptanceTestingTransactionalStorageFeature>();

        EnableByDefault<AcceptanceTestingTransactionalStorageFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context) => context.Services.AddSingleton<ISagaPersister, AcceptanceTestingSagaPersister>();
}