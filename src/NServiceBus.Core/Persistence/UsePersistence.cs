namespace NServiceBus.Persistence
{
    using System;
    using System.Linq;
    using Settings;


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
            var type =
              Configure.TypesToScan.SingleOrDefault(
                  t => typeof(IConfigurePersistence<>).MakeGenericType(definitionType).IsAssignableFrom(t));

            if (type == null)
                throw new InvalidOperationException(
                    "We couldn't find a IConfigurePersistence implementation for your selected persistence: " +
                    definitionType.Name);

            if (customizations != null)
            {
                customizations(new PersistenceConfiguration());
            }

            ((IConfigurePersistence)Activator.CreateInstance(type)).Enable(config);

            return config;
        }

        public static void DefaultTo<T>() where T : PersistenceDefinition
        {

            SettingsHolder.Instance.SetDefault("DefaultPersistence",typeof(T));
        }
    }

    /// <summary>
    /// Base class for persistence definitions
    /// </summary>
    public class PersistenceDefinition{}

    /// <summary>
    /// Provides a hook for extention methods in order tp provide custom configuration methods
    /// </summary>
    public class PersistenceConfiguration{}

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