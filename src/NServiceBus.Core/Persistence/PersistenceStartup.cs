namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Utils.Reflection;

    class PersistenceStartup : IWantToRunBeforeConfigurationIsFinalized
    {

        static ILog Logger = LogManager.GetLogger(typeof(PersistenceStartup));

        public void Run(Configure config)
        {
            var settings = config.Settings;

            List<EnabledPersistence> definitions;
            if (!settings.TryGet("PersistenceDefinitions", out definitions))
            {
                DefaultToInMemory(settings);
                return;
            }

            definitions.Reverse();
            var availableStorages = Reflect<Storage>.GetEnumValues();
            var resultingSupportedStorages = new List<Storage>();
            foreach (var definition in definitions)
            {
                var persistenceDefinition = definition.DefinitionType.Construct<PersistenceDefinition>();
                var supportedStorages = persistenceDefinition.GetSupportedStorages(settings, definition.Customizations);
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
            SetResultingSupportedStorages(settings, resultingSupportedStorages);
        }

        static void DefaultToInMemory(SettingsHolder settings)
        {
            ThrowOrLogForInMemory();
            var inMemory = new InMemory();
            var allStorages = Reflect<Storage>.GetEnumValues();
            foreach (var storage in allStorages)
            {
                inMemory.ApplyActionForStorage(storage, settings);
            }
            SetResultingSupportedStorages(settings,allStorages);
        }

        static void ThrowOrLogForInMemory()
        {
            if (SystemInformation.UserInteractive)
            {
                const string warningMessage = "No persistence has been selected, NServiceBus will now use InMemory persistence. We recommend that you change the persistence before deploying to production. To do this,  please add a call to config.UsePersistence<T>() where T can be any of the supported persistence options supported. http://docs.particular.net/nservicebus/persistence-in-nservicebus.";
                Logger.Warn(warningMessage);
            }
            else
            {
                const string errorMessage = "No persistence has been selected, please add a call to config.UsePersistence<T>() where T can be any of the supported persistence options supported. http://docs.particular.net/nservicebus/persistence-in-nservicebus";
                throw new Exception(errorMessage);
            }
        }

        static void SetResultingSupportedStorages(SettingsHolder settings, List<Storage> supportedStorages)
        {
            settings.Set("ResultingSupportedStorages", supportedStorages);
        }

        internal static bool HasSupportFor(ReadOnlySettings settings, Storage storages)
        {
            return settings.Get<List<Storage>>("ResultingSupportedStorages")
                .Contains(storages);
        }

    }
}