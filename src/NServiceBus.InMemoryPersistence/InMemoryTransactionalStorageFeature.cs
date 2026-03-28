namespace NServiceBus.Persistence.InMemory;

using Features;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

class InMemoryTransactionalStorageFeature : Feature
{
    public InMemoryTransactionalStorageFeature() => DependsOn<SynchronizedStorage>();

    protected override void Setup(FeatureConfigurationContext context)
        => context.Services.AddScoped<ICompletableSynchronizedStorageSession, InMemorySynchronizedStorageSession>();
}
