namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Features;
using Logging;
using Settings;

static class PersistenceComponent
{
    public static void ConfigurePersistence(this SettingsHolder settings)
    {
        if (!settings.TryGet<PersistenceRegistry>(out var persistenceRegistry))
        {
            return;
        }

        var enabledPersistences = persistenceRegistry.Merge();

        settings.ValidateSagaAndOutboxUseSamePersistence(enabledPersistences);

        var resultingSupportedStorages = new List<StorageType>();
        var diagnostics = new Dictionary<string, object>();

        foreach (var enabledPersistence in enabledPersistences)
        {
            var persistenceDefinition = enabledPersistence.Definition;
            persistenceDefinition.ApplyDefaults(settings);

            var definitionType = persistenceDefinition.GetType();
            foreach (var storageType in enabledPersistence.SelectedStorages)
            {
                Logger.DebugFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", definitionType.Name, storageType);
                persistenceDefinition.ApplyActionForStorage(storageType, settings);
                resultingSupportedStorages.Add(storageType);

                diagnostics.Add(storageType.ToString(), new
                {
                    Type = definitionType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(definitionType)
                });
            }
        }

        settings.Set<IReadOnlyCollection<StorageType>>(resultingSupportedStorages);

        settings.AddStartupDiagnosticsSection("Persistence", diagnostics);
    }

    static void ValidateSagaAndOutboxUseSamePersistence(this SettingsHolder settings, IReadOnlyCollection<EnabledPersistence> enabledPersistences)
    {
        var sagaPersisterType = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains(StorageType.Sagas.Instance));
        var outboxPersisterType = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains(StorageType.Outbox.Instance));
        var bothFeaturesEnabled = settings.IsFeatureEnabled(typeof(Features.Sagas)) && settings.IsFeatureEnabled(typeof(Features.Outbox));

        if (sagaPersisterType != null
            && outboxPersisterType != null
            && sagaPersisterType.Definition != outboxPersisterType.Definition
            && bothFeaturesEnabled)
        {
            throw new Exception($"Sagas and the Outbox need to use the same type of persistence. Saga persistence is configured to use {sagaPersisterType.Definition.GetType().Name}. Outbox persistence is configured to use {outboxPersisterType.GetType().Name}.");
        }
    }

    internal static bool HasSupportFor<T>(this IReadOnlySettings settings) where T : StorageType
    {
        _ = settings.TryGet(out IReadOnlyCollection<StorageType> supportedStorages);

        return supportedStorages?.Contains(StorageType.Get<T>()) ?? false;
    }

    static readonly ILog Logger = LogManager.GetLogger(typeof(PersistenceComponent));
}