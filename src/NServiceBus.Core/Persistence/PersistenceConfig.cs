namespace NServiceBus.Persistence
{
    using System;

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

            config.UsePersistence(typeof(T));

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