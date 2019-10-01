namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using Persistence;
    using Settings;

    class PersistenceStartup : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(SettingsHolder settings)
        {
            if (!settings.TryGet("PersistenceDefinitions", out List<EnabledPersistence> definitions))
            {
                return;
            }

            var enabledPersistences = PersistenceStorageMerger.Merge(definitions, settings);

            var resultingSupportedStorages = new List<Type>();
            var diagnostics = new Dictionary<string, object>();

            ValidateSagaAndOutboxPersistence(enabledPersistences);

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

            settings.Set("ResultingSupportedStorages", resultingSupportedStorages);

            settings.AddStartupDiagnosticsSection("Persistence", diagnostics);
        }

        static void ValidateSagaAndOutboxPersistence(List<EnabledPersistence> enabledPersistences)
        {
            var sagaPersisterType = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains(typeof(StorageType.Sagas)));
            var outboxPersisterType = enabledPersistences.FirstOrDefault(p => p.SelectedStorages.Contains(typeof(StorageType.Outbox)));

            if (sagaPersisterType != null 
                && outboxPersisterType != null
                && sagaPersisterType.DefinitionType != outboxPersisterType.DefinitionType)
            {
                throw new Exception($"Sagas and Outbox need to use the same type of persistence. Saga is configured to use {sagaPersisterType.DefinitionType.Name}. Outbox is configured to use {outboxPersisterType.DefinitionType.Name}.");
            }
        }

        internal static bool HasSupportFor<T>(ReadOnlySettings settings) where T : StorageType
        {
            settings.TryGet("ResultingSupportedStorages", out List<Type> supportedStorages);

            return supportedStorages?.Contains(typeof(T)) ?? false;
        }

        static ILog Logger = LogManager.GetLogger(typeof(PersistenceStartup));
    }
}