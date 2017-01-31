namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Persistence;
    using Settings;

    class PersistenceStartup : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(SettingsHolder settings)
        {
            List<EnabledPersistence> definitions;

            if (!settings.TryGet("PersistenceDefinitions", out definitions))
            {
                return;
            }

            var enabledPersistences = PersistenceStorageMerger.Merge(definitions, settings);

            var resultingSupportedStorages = new List<Type>();

            foreach (var definition in enabledPersistences)
            {
                var persistenceDefinition = definition.DefinitionType.Construct<PersistenceDefinition>();

                persistenceDefinition.ApplyDefaults(settings);

                foreach (var storageType in definition.SelectedStorages)
                {
                    Logger.DebugFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", definition.DefinitionType.Name, storageType);
                    persistenceDefinition.ApplyActionForStorage(storageType, settings);
                    resultingSupportedStorages.Add(storageType);
                }
            }

            settings.Set("ResultingSupportedStorages", resultingSupportedStorages);
        }

        internal static bool HasSupportFor<T>(ReadOnlySettings settings) where T : StorageType
        {
            List<Type> supportedStorages;
            settings.TryGet("ResultingSupportedStorages", out supportedStorages);

            return supportedStorages?.Contains(typeof(T)) ?? false;
        }

        static ILog Logger = LogManager.GetLogger(typeof(PersistenceStartup));
    }
}