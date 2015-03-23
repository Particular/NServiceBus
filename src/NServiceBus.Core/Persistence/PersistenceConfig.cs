namespace NServiceBus
{
    using System;
    using NServiceBus.Persistence;

    /// <summary>
    /// Enables users to select persistence by calling .UsePersistence()
    /// </summary>
    public static class PersistenceConfig
    {
        /// <summary>
        /// Configures the given persistence to be used
        /// </summary>
        /// <typeparam name="T">The persistence definition eg <see cref="InMemoryPersistence"/>, NHibernate etc</typeparam>
        /// <param name="config">The configuration object since this is an extention method</param>
        public static PersistenceExtentions<T> UsePersistence<T>(this BusConfiguration config) where T : PersistenceDefinition
        {
            Guard.AgainstNull(config, "config");
            var type = typeof(PersistenceExtentions<>).MakeGenericType(typeof(T));
            return (PersistenceExtentions<T>)Activator.CreateInstance(type, config.Settings);
        }

        /// <summary>
        /// Configures the given persistence to be used for a specific storage type
        /// </summary>
        /// <typeparam name="T">The persistence definition eg <see cref="InMemoryPersistence"/>, NHibernate etc</typeparam>
        /// <typeparam name="S">The <see cref="StorageType"/>storage type</typeparam>
        /// <param name="config">The configuration object since this is an extention method</param>
        public static PersistenceExtentions<T, S> UsePersistence<T, S>(this BusConfiguration config) where T : PersistenceDefinition
                                                                                                     where S : StorageType
        {
            Guard.AgainstNull(config, "config");
            var type = typeof(PersistenceExtentions<,>).MakeGenericType(typeof(T), typeof(S));
            return (PersistenceExtentions<T, S>) Activator.CreateInstance(type, config.Settings);
        }

        /// <summary>
        ///  Configures the given persistence to be used
        /// </summary>
        /// <param name="config">The configuration object since this is an extention method</param>
        /// <param name="definitionType">The persistence definition eg <see cref="InMemoryPersistence"/>, NHibernate etc</param>
        public static PersistenceExtentions UsePersistence(this BusConfiguration config, Type definitionType)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(definitionType, "definitionType");
            return new PersistenceExtentions(definitionType, config.Settings, null);
        }
    }
}