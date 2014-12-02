namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Utils.Reflection;

    class PersistenceStartup : IWantToRunBeforeConfigurationIsFinalized
    {
        const string errorMessage = "No persistence has been selected, please select your persistence by calling configuration.UsePersistence<T>() in your class that implements either IConfigureThisEndpoint or INeedInitialization, where T can be any of the supported persistence option. If you were previously using RavenDB, note that it has been moved to its own stand alone nuget 'NServiceBus.RavenDB' and you'll need to install this package and then call configuration.UsePersistence<RavenDBPersistence>()";

        static ILog Logger = LogManager.GetLogger(typeof(PersistenceStartup));

        public void Run(Configure config)
        {
            var settings = config.Settings;

            if (settings.Get<bool>("Endpoint.SendOnly"))
            {
                return;
            }

            List<EnabledPersistence> definitions;
            if (!settings.TryGet("PersistenceDefinitions", out definitions))
            {
                throw new Exception(errorMessage);
            }

            definitions.Reverse();

            var availableStorages = StorageType.GetAvailableStorageTypes();
            var resultingSupportedStorages = new List<Type>();

            foreach (var definition in definitions)
            {
                var persistenceDefinition = definition.DefinitionType.Construct<PersistenceDefinition>();
                var supportedStorages = persistenceDefinition.GetSupportedStorages(definition.SelectedStorages);

                persistenceDefinition.ApplyDefaults(settings);

                foreach (var storageType in supportedStorages)
                {
                    if (availableStorages.Contains(storageType))
                    {
                        Logger.InfoFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", definition.DefinitionType.Name, storageType);
                        availableStorages.Remove(storageType);
                        persistenceDefinition.ApplyActionForStorage(storageType, settings);
                        resultingSupportedStorages.Add(storageType);
                    }
                    else
                    {
                        Logger.InfoFormat("Persistence '{0}' was not applied to storage '{1}' since that storage has been claimed by another persistence. This is a 'last one wins' scenario.", definition.DefinitionType.Name, storageType);
                    }
                }
            }

            settings.Set("ResultingSupportedStorages", resultingSupportedStorages);
        }

        internal static bool HasSupportFor<T>(ReadOnlySettings settings) where T : StorageType
        {
            return settings.Get<List<Type>>("ResultingSupportedStorages")
                .Contains(typeof(T));
        }
    }
}
