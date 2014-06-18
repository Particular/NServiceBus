namespace NServiceBus.Persistence
{
    using System;
    using System.Linq;
    using Utils.Reflection;


    /// <summary>
    /// Enables users to select persistence by calling .UsePersistence()
    /// </summary>
    public static class PersistenceConfig
    {
        /// <summary>
        /// Configures the given persistence to be used
        /// </summary>
        /// <typeparam name="T">The persistence definition eg InMemory , NHibernate etc</typeparam>
        /// <param name="config">The configuration object since this is an extention method</param>
        /// <param name="customizations">Any customizations needed</param>
        /// <returns></returns>
        public static Configure UsePersistence<T>(this Configure config, Action<PersistenceConfiguration> customizations = null) where T : PersistenceDefinition
        {
            return UsePersistence(config, typeof(T), customizations);
        }

        /// <summary>
        ///  Configures the given persistence to be used
        /// </summary>
        /// <param name="config">The configuration object since this is an extention method</param>
        /// <param name="definitionType">The persistence definition eg InMemory , NHibernate etc</param>
        /// <param name="customizations">Any customizations needed</param>
        /// <returns></returns>
        public static Configure UsePersistence(this Configure config, Type definitionType, Action<PersistenceConfiguration> customizations = null)
        {
            EnabledPersistences enabledPersistences;

            if (!config.Settings.TryGet(out enabledPersistences))
            {
                enabledPersistences = new EnabledPersistences();
                config.Settings.Set<EnabledPersistences>(enabledPersistences);
            }

            var supportedStorages = definitionType.Construct<PersistenceDefinition>().SupportedStorages.ToList();

            if (customizations == null)
            {
                enabledPersistences.AddWildcardRegistration(definitionType, supportedStorages);
                return config;
            }

            var persistenceConfiguration = new PersistenceConfiguration(config);

            customizations(persistenceConfiguration);

            if (persistenceConfiguration.SpecificStorages.Any())
            {
                enabledPersistences.ClaimStorages(definitionType, persistenceConfiguration.SpecificStorages);
            }
            else
            {
                enabledPersistences.AddWildcardRegistration(definitionType, supportedStorages);
            }

            return config;
        }
    }
}