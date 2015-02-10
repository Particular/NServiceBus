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
                    Logger.InfoFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", definition.DefinitionType.Name, storageType);
                    persistenceDefinition.ApplyActionForStorage(storageType, settings);
                    resultingSupportedStorages.Add(storageType);
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
