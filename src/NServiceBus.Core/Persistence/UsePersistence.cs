namespace NServiceBus.Persistence
{
    using System;


    /// <summary>
    /// Enables users to select persistence by calling .UsePersistence()
    /// </summary>
    public static class PersistenceConfig
    {
        public static Configure UsePersistence<T>(this Configure config, Action<PersistenceConfiguration> customizations = null) where T : PersistenceDefinition
        {
            return UsePersistence(config, typeof(T), customizations);
        }

        public static Configure UsePersistence(this Configure config, Type definitionType, Action<PersistenceConfiguration> customizations = null)
        {   
            if (customizations != null)
            {
                customizations(new PersistenceConfiguration(config));
            }

            config.Settings.Set("Persistence", definitionType);

            return config;
        }
    }
}