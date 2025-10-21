#nullable enable

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

            foreach (var storageType in enabledPersistence.SelectedStorages)
            {
                Logger.DebugFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", persistenceDefinition.Name, storageType);
                settings.EnableFeatureByDefault(persistenceDefinition.GetFeatureForStorage(storageType));
                resultingSupportedStorages.Add(storageType);

                diagnostics.Add(storageType.ToString(), new
                {
                    Type = persistenceDefinition.FullName,
                    Version = FileVersionRetriever.GetFileVersion(persistenceDefinition.GetType())
                });
            }
        }

        settings.Set<IReadOnlyCollection<StorageType>>(resultingSupportedStorages);

        settings.AddStartupDiagnosticsSection("Persistence", diagnostics);
    }

    static void ValidateSagaAndOutboxUseSamePersistence(this SettingsHolder settings, IReadOnlyCollection<EnabledPersistence> enabledPersistences)
    {
        var sagaPersisterDefinition = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains<StorageType.Sagas>())?.Definition;
        var outboxPersisterDefinition = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains<StorageType.Outbox>())?.Definition;
        var bothFeaturesEnabled = settings.IsFeatureEnabled<Features.Sagas>() && settings.IsFeatureEnabled<Features.Outbox>();

        if (sagaPersisterDefinition != null
            && outboxPersisterDefinition != null
            && sagaPersisterDefinition != outboxPersisterDefinition
            && bothFeaturesEnabled)
        {
            throw new Exception($"Sagas and the Outbox need to use the same type of persistence. Saga persistence is configured to use '{sagaPersisterDefinition.Name}'. Outbox persistence is configured to use '{outboxPersisterDefinition.Name}'.");
        }
    }

    internal static bool HasSupportFor<T>(this IReadOnlySettings settings) where T : StorageType
    {
        _ = settings.TryGet(out IReadOnlyCollection<StorageType> supportedStorages);

        return supportedStorages.Contains<T>();
    }

    static readonly ILog Logger = LogManager.GetLogger(typeof(PersistenceComponent));
}