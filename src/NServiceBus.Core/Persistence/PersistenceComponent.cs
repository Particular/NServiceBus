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

sealed class PersistenceComponent(PersistenceComponent.Settings settings)
{
    public void Initialize(SettingsHolder settingsHolder)
    {
        if (settings.Enabled.Count == 0)
        {
            return;
        }

        var resultingSupportedStorages = new List<StorageType>();
        var diagnostics = new Dictionary<string, object>();

        foreach (var enabledPersistence in settings.Enabled)
        {
            var persistenceDefinition = enabledPersistence.Definition;
            persistenceDefinition.ApplyDefaults(settingsHolder);

            foreach (var storageType in enabledPersistence.SelectedStorages)
            {
                Logger.DebugFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", persistenceDefinition.Name, storageType);
                persistenceDefinition.Apply(storageType, settingsHolder.Get<FeatureComponent.Settings>());
                resultingSupportedStorages.Add(storageType);

                diagnostics.Add(storageType.ToString(), new
                {
                    Type = persistenceDefinition.FullName,
                    Version = FileVersionRetriever.GetFileVersion(persistenceDefinition.GetType())
                });
            }
        }

        SupportedStorages = resultingSupportedStorages;
        settingsHolder.AddStartupDiagnosticsSection("Persistence", diagnostics);
    }

    public void AssertSagaAndOutboxUseSamePersistence(IReadOnlySettings readOnlySettings)
    {
        var enabledPersistences = settings.Enabled;
        var sagaPersisterDefinition = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains<StorageType.Sagas>())?.Definition;
        var outboxPersisterDefinition = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains<StorageType.Outbox>())?.Definition;
        var bothFeaturesActive = readOnlySettings.IsFeatureActive<Features.Sagas>() && readOnlySettings.IsFeatureActive<Features.Outbox>();

        if (sagaPersisterDefinition != null
            && outboxPersisterDefinition != null
            && sagaPersisterDefinition != outboxPersisterDefinition
            && bothFeaturesActive)
        {
            throw new Exception($"Sagas and the Outbox need to use the same type of persistence. Saga persistence is configured to use '{sagaPersisterDefinition.Name}'. Outbox persistence is configured to use '{outboxPersisterDefinition.Name}'.");
        }
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

    public IReadOnlyCollection<StorageType> SupportedStorages { get; private set; }
}