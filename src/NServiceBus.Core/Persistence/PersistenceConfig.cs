namespace NServiceBus
{
    using System;
    using Persistence;

    /// <summary>
    /// Enables users to select persistence by calling .UsePersistence().
    /// </summary>
    public static class PersistenceConfig
    {
        /// <summary>
        /// Configures the given persistence to be used.
        /// </summary>
        /// <typeparam name="T">The persistence definition eg <see cref="InMemoryPersistence" />, NHibernate etc.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static PersistenceExtensions<T> UsePersistence<T>(this EndpointConfiguration config) where T : PersistenceDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            var type = typeof(PersistenceExtensions<>).MakeGenericType(typeof(T));
            return (PersistenceExtensions<T>) Activator.CreateInstance(type, config.Settings);
        }

        /// <summary>
        /// Configures the given persistence to be used for a specific storage type.
        /// </summary>
        /// <typeparam name="T">The persistence definition eg <see cref="InMemoryPersistence" />, NHibernate etc.</typeparam>
        /// <typeparam name="S">The <see cref="StorageType" />storage type.</typeparam>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static PersistenceExtensions<T, S> UsePersistence<T, S>(this EndpointConfiguration config) where T : PersistenceDefinition
            where S : StorageType
        {
            Guard.AgainstNull(nameof(config), config);
            var type = typeof(PersistenceExtensions<,>).MakeGenericType(typeof(T), typeof(S));
            return (PersistenceExtensions<T, S>) Activator.CreateInstance(type, config.Settings);
        }

        /// <summary>
        /// Configures the given persistence to be used.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="definitionType">The persistence definition eg <see cref="InMemoryPersistence" />, NHibernate etc.</param>
        public static PersistenceExtensions UsePersistence(this EndpointConfiguration config, Type definitionType)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(definitionType), definitionType);
            return new PersistenceExtensions(definitionType, config.Settings, null);
        }
    }
}