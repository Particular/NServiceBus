﻿namespace NServiceBus.Persistence
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

            var availableStorages = Reflect<Storage>.GetEnumValues();
            var resultingSupportedStorages = new List<Storage>();

            foreach (var definition in definitions)
            {
                var persistenceDefinition = definition.DefinitionType.Construct<PersistenceDefinition>();
                var supportedStorages = persistenceDefinition.GetSupportedStorages(definition.SelectedStorages);

                persistenceDefinition.ApplyDefaults(settings);

                foreach (var storage in supportedStorages)
                {
                    if (availableStorages.Contains(storage))
                    {
                        Logger.InfoFormat("Activating persistence '{0}' to provide storage for '{1}' storage.", definition.DefinitionType.Name, storage);
                        availableStorages.Remove(storage);
                        persistenceDefinition.ApplyActionForStorage(storage, settings);
                        resultingSupportedStorages.Add(storage);
                    }
                    else
                    {
                        Logger.InfoFormat("Persistence '{0}' was not applied to storage '{1}' since that storage has been claimed by another persistence. This is a 'last one wins' scenario.", definition.DefinitionType.Name, storage);
                    }
                }
            }

            settings.Set("ResultingSupportedStorages", resultingSupportedStorages);
        }

        internal static bool HasSupportFor(ReadOnlySettings settings, Storage storages)
        {
            return settings.Get<List<Storage>>("ResultingSupportedStorages")
                .Contains(storages);
        }
    }
}
