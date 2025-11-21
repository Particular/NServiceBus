#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Features;
using Logging;
using Persistence;
using Settings;

sealed class PersistenceComponent(PersistenceComponent.Settings persistenceSettings)
{
    public Configuration Initialize(SettingsHolder settings)
    {
        if (persistenceSettings.Enabled.Count == 0)
        {
            return new Configuration(settings, persistenceSettings.Enabled, []);
        }

        var resultingSupportedStorages = new List<(StorageType Storage, StorageType.Options Options)>();
        var diagnostics = new Dictionary<string, object>();

        foreach (var enabledPersistence in persistenceSettings.Enabled)
        {
            var persistenceDefinition = enabledPersistence.Definition;
            persistenceDefinition.ApplyDefaults(settings);

            foreach (var storageType in enabledPersistence.SelectedStorages)
            {
                Logger.DebugFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", persistenceDefinition.Name, storageType.Storage);
                persistenceDefinition.Apply(storageType.Storage, settings.Get<FeatureComponent.Settings>());
                resultingSupportedStorages.Add(storageType);

                diagnostics.Add(storageType.Storage.ToString(), new
                {
                    Type = persistenceDefinition.FullName,
                    Version = FileVersionRetriever.GetFileVersion(persistenceDefinition.GetType())
                });
            }
        }

        settings.AddStartupDiagnosticsSection("Persistence", diagnostics);

        return new Configuration(settings, persistenceSettings.Enabled, resultingSupportedStorages);
    }

    static readonly ILog Logger = LogManager.GetLogger(typeof(PersistenceComponent));

    public class Settings
    {
        PersistenceRegistry? persistenceRegistry;

        [field: AllowNull, MaybeNull]
        public IReadOnlyCollection<EnabledPersistence> Enabled
        {
            get
            {
                if (persistenceRegistry == null)
                {
                    return [];
                }

                field ??= persistenceRegistry.Merge();
                return field;
            }
        }

        public void Enable<T>(StorageType? storageType) where T : PersistenceDefinition, IPersistenceDefinitionFactory<T>
        {
            persistenceRegistry ??= new PersistenceRegistry();
            var enable = persistenceRegistry.Enable<T>();
            if (storageType is not null)
            {
                enable.WithStorage(storageType);
            }
        }
    }

    internal class Configuration(IReadOnlySettings settings, IReadOnlyCollection<EnabledPersistence> enabledPersistences, IReadOnlyCollection<(StorageType Storage, StorageType.Options Options)> supportedPersistences)
    {
        public IReadOnlyCollection<(StorageType Storage, StorageType.Options Options)> SupportedPersistences { get; } = supportedPersistences;

        public void AssertSagaAndOutboxUseSamePersistence()
        {
            var sagaPersisterDefinition = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains<StorageType.Sagas>())?.Definition;
            var outboxPersisterDefinition = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains<StorageType.Outbox>())?.Definition;
            var bothFeaturesActive = settings.IsFeatureActive<Features.Sagas>() && settings.IsFeatureActive<Features.Outbox>();

            if (sagaPersisterDefinition != null
                && outboxPersisterDefinition != null
                && sagaPersisterDefinition != outboxPersisterDefinition
                && bothFeaturesActive)
            {
                throw new Exception($"Sagas and the Outbox need to use the same type of persistence. Saga persistence is configured to use '{sagaPersisterDefinition.Name}'. Outbox persistence is configured to use '{outboxPersisterDefinition.Name}'.");
            }
        }
    }
}