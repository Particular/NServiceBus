#nullable enable

namespace NServiceBus.Features;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Sagas;

sealed class LearningSagaPersistence : Feature
{
    public LearningSagaPersistence()
    {
        Defaults(s =>
        {
            s.Set<ISagaIdGenerator>(new LearningSagaIdGenerator());
            s.SetDefault(StorageLocationKey, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".sagas"));
        });

        Enable<LearningSynchronizedStorage>();

        DependsOn<Sagas>();
        DependsOn<LearningSynchronizedStorage>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var storageLocation = context.Settings.Get<string>(StorageLocationKey);
        var serializerOptions = context.Settings.GetOrDefault<JsonSerializerOptions>(SerializerOptionsKey) ?? GetDefaultOptions();

        var allSagas = context.Settings.Get<SagaMetadataCollection>();

        context.Services.AddSingleton(new SagaManifestCollection(allSagas, storageLocation, sagaName => sagaName.Replace("+", ""), serializerOptions));
        context.Services.AddSingleton<ISagaPersister, LearningSagaPersister>();
    }

    static JsonSerializerOptions GetDefaultOptions()
    {
        var options = new JsonSerializerOptions();
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.Converters.Add(CreateDefaultConverter());
        }
        return options;
    }

    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "Only used when System.Text.Json reflection is enabled.")]
    static JsonStringEnumConverter CreateDefaultConverter() => new();

    internal static readonly string StorageLocationKey = "LearningSagaPersistence.StorageLocation";
    internal static readonly string SerializerOptionsKey = "LearningSagaPersistence.SerializerOptions";
}