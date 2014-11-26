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
            var type = typeof(PersistenceExtentions<>).MakeGenericType(typeof(T));
            var extension = (PersistenceExtentions<T>)Activator.CreateInstance(type, config.Settings);

            return extension;
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
            var type = typeof(PersistenceExtentions<,>).MakeGenericType(typeof(T), typeof(S));
            var extension = (PersistenceExtentions<T, S>) Activator.CreateInstance(type, config.Settings);

            return extension;
        }

        /// <summary>
        ///  Configures the given persistence to be used
        /// </summary>
        /// <param name="config">The configuration object since this is an extention method</param>
        /// <param name="definitionType">The persistence definition eg <see cref="InMemoryPersistence"/>, NHibernate etc</param>
        public static PersistenceExtentions UsePersistence(this BusConfiguration config, Type definitionType)
        {
            return new PersistenceExtentions(definitionType, config.Settings);
        }
    }
}