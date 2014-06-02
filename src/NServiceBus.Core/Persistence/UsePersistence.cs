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

    /// <summary>
    /// Base class for persistence definitions
    /// </summary>
    public class PersistenceDefinition{}

    /// <summary>
    /// Provides a hook for extention methods in order tp provide custom configuration methods
    /// </summary>
    public class PersistenceConfiguration{
        public Configure Config { get; private set; }

        public PersistenceConfiguration(Configure config)
        {
            Config = config;
        }
    }

    /// <summary>
    /// Enables the given persistence using the default settings
    /// </summary>
    public interface IConfigurePersistence
    {
        void Enable(Configure config);
    }


    /// <summary>
    /// The generic counterpart to IConfigurePersistence
    /// </summary>
    public interface IConfigurePersistence<T> : IConfigurePersistence where T : PersistenceDefinition { }
}