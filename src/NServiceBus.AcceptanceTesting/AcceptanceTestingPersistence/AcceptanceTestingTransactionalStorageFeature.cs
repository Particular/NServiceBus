namespace NServiceBus.AcceptanceTesting;

using Features;
using Persistence;
using Microsoft.Extensions.DependencyInjection;

class AcceptanceTestingTransactionalStorageFeature : Feature
{
    public AcceptanceTestingTransactionalStorageFeature() => DependsOn<SynchronizedStorage>();

    protected internal override void Setup(FeatureConfigurationContext context)
        => context.Services.AddScoped<ICompletableSynchronizedStorageSession, AcceptanceTestingSynchronizedStorageSession>();
}