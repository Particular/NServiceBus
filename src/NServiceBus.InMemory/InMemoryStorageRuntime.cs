namespace NServiceBus.Persistence.InMemory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

static class InMemoryStorageRuntime
{
    public const string StorageKey = "InMemoryPersistence.Storage";

    public static InMemoryStorage SharedStorage { get; } = new();

    public static void Configure(IServiceCollection services, InMemoryStorage? configuredStorage)
        => services.TryAddSingleton<InMemoryStorage>(_ => configuredStorage ?? SharedStorage);
}