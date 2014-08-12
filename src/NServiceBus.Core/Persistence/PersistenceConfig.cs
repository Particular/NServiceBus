namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Enables users to select persistence by calling .UsePersistence()
    /// </summary>
    public static partial class PersistenceConfig
    {

        /// <summary>
        /// Configures the given persistence to be used
        /// </summary>
        /// <typeparam name="T">The persistence definition eg <see cref="InMemory"/>, NHibernate etc</typeparam>
        /// <param name="config">The configuration object since this is an extention method</param>
        /// <param name="customizations">Any customizations needed</param>
        public static ConfigurationBuilder UsePersistence<T>(this ConfigurationBuilder config, Action<PersistenceConfiguration> customizations = null) where T : PersistenceDefinition
        {
            return UsePersistence(config, typeof(T), customizations);
        }

        /// <summary>
        ///  Configures the given persistence to be used
        /// </summary>
        /// <param name="config">The configuration object since this is an extention method</param>
        /// <param name="definitionType">The persistence definition eg <see cref="InMemory"/>, NHibernate etc</param>
        /// <param name="customizations">Any customizations needed</param>
        public static ConfigurationBuilder UsePersistence(this ConfigurationBuilder config, Type definitionType, Action<PersistenceConfiguration> customizations = null)
        {
            var settings = config.settings;
            List<EnabledPersistence> definitions;
            if (!settings.TryGet("PersistenceDefinitions", out definitions))
            {
                definitions = new List<EnabledPersistence>();
                settings.Set("PersistenceDefinitions", definitions);
            }

            definitions.Add(new EnabledPersistence
            {
                DefinitionType = definitionType,
                Customizations = customizations
            });
            return config;
        }

        
    }
}