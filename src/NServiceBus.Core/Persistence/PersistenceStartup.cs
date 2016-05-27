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
                if (settings.Get<bool>("Endpoint.SendOnly"))
                {
                    return;
                }

                throw new Exception(errorMessage);
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

        const string errorMessage = "No persistence has been selected, select a persistence by calling endpointConfiguration.UsePersistence<T>() in the class that implements either IConfigureThisEndpoint or INeedInitialization, where T can be any of the supported persistence option. If previously using RavenDB, note that it has been moved to its own stand alone nuget 'NServiceBus.RavenDB'. This package will need to be installed and then enabled by calling endpointConfiguration.UsePersistence<RavenDBPersistence>().";

        static ILog Logger = LogManager.GetLogger(typeof(PersistenceStartup));
    }
}