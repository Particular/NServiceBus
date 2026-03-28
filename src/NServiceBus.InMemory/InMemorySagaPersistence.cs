namespace NServiceBus.Persistence.InMemory;

using System.Text.Json;
using System.Text.Json.Serialization;
using Features;
using Microsoft.Extensions.DependencyInjection;
using Sagas;

class InMemorySagaPersistence : Feature
{
    public InMemorySagaPersistence()
    {
        DependsOn<Sagas>();
        DependsOn<InMemoryTransactionalStorageFeature>();

        Enable<InMemoryTransactionalStorageFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var configuredStorage = context.Settings.GetOrDefault<InMemoryStorage>(InMemoryStorageRuntime.StorageKey);
        InMemoryStorageRuntime.Configure(context.Services, configuredStorage);

        var serializerOptions = context.Settings.GetOrDefault<JsonSerializerOptions>(SerializerOptionsKey)
            ?? new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

        context.Services.AddSingleton(new InMemorySagaPersisterSettings(serializerOptions));
        context.Services.AddSingleton<InMemorySagaPersister>(sp =>
            new InMemorySagaPersister(
                sp.GetRequiredService<InMemoryStorage>(),
                sp.GetRequiredService<InMemorySagaPersisterSettings>()));
        context.Services.AddSingleton<ISagaPersister>(sp => sp.GetRequiredService<InMemorySagaPersister>());
    }

    internal static readonly string SerializerOptionsKey = "InMemorySagaPersistence.SerializerOptions";
}