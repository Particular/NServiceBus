namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Features;
using Logging;
using Persistence;
using Settings;

class PersistenceComponent
{
    public static void Configure(SettingsHolder settings)
    {
        if (!settings.TryGet(PersistenceDefinitionsSettingsKey, out List<EnabledPersistence> definitions))
        {
            return;
        }

        var enabledPersistences = PersistenceStorageMerger.Merge(definitions, settings);

        ValidateSagaAndOutboxUseSamePersistence(enabledPersistences, settings);

        var resultingSupportedStorages = new List<Type>();
        var diagnostics = new Dictionary<string, object>();

        foreach (var definition in enabledPersistences)
        {
            var persistenceDefinition = definition.DefinitionType.Construct<PersistenceDefinition>();

            persistenceDefinition.ApplyDefaults(settings);

            foreach (var storageType in definition.SelectedStorages)
            {
                Logger.DebugFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", definition.DefinitionType.Name, storageType);
                persistenceDefinition.ApplyActionForStorage(storageType, settings);
                resultingSupportedStorages.Add(storageType);

                diagnostics.Add(storageType.Name, new
                {
                    Type = definition.DefinitionType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(definition.DefinitionType)
                });
            }
        }

        settings.Set(ResultingSupportedStoragesSettingsKey, resultingSupportedStorages);

        settings.AddStartupDiagnosticsSection("Persistence", diagnostics);
    }

    static void ValidateSagaAndOutboxUseSamePersistence(List<EnabledPersistence> enabledPersistences, SettingsHolder settings)
    {
        var sagaPersisterType = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains(typeof(StorageType.Sagas)));
        var outboxPersisterType = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains(typeof(StorageType.Outbox)));
        var bothFeaturesEnabled = settings.IsFeatureEnabled(typeof(Features.Sagas)) && settings.IsFeatureEnabled(typeof(Features.Outbox));

        if (sagaPersisterType != null
            && outboxPersisterType != null
            && sagaPersisterType.DefinitionType != outboxPersisterType.DefinitionType
            && bothFeaturesEnabled)
        {
            throw new Exception($"Sagas and the Outbox need to use the same type of persistence. Saga persistence is configured to use {sagaPersisterType.DefinitionType.Name}. Outbox persistence is configured to use {outboxPersisterType.DefinitionType.Name}.");
        }
    }

    internal static bool HasSupportFor<T>(IReadOnlySettings settings) where T : StorageType
    {
        settings.TryGet(ResultingSupportedStoragesSettingsKey, out List<Type> supportedStorages);

        return supportedStorages?.Contains(typeof(T)) ?? false;
    }

    internal const string PersistenceDefinitionsSettingsKey = "PersistenceDefinitions";

    const string ResultingSupportedStoragesSettingsKey = "ResultingSupportedStorages";

    static readonly ILog Logger = LogManager.GetLogger(typeof(PersistenceComponent));
}