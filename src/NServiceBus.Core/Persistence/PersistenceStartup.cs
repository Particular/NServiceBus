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

        internal static bool HasSupportFor<T>(ReadOnlySettings settings) where T : StorageType
        {
            settings.TryGet("ResultingSupportedStorages", out List<Type> supportedStorages);

            return supportedStorages?.Contains(typeof(T)) ?? false;
        }

        static ILog Logger = LogManager.GetLogger(typeof(PersistenceStartup));
    }
}