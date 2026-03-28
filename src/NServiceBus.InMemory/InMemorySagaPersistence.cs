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
        var serializerOptions = context.Settings.GetOrDefault<JsonSerializerOptions>(SerializerOptionsKey)
            ?? new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

        context.Services.AddSingleton(new InMemorySagaPersisterSettings(serializerOptions));
        context.Services.AddSingleton<ISagaPersister, InMemorySagaPersister>();
    }

    internal static readonly string SerializerOptionsKey = "InMemorySagaPersistence.SerializerOptions";
}