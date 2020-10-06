namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvancedExtensibility;
    using Persistence;
    using Settings;

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings for specific storage
    /// type via extension methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="InMemoryPersistence" />, etc.</typeparam>
    /// <typeparam name="S">The <see cref="StorageType" />storage type.</typeparam>
    public class PersistenceExtensions<T, S> : PersistenceExtensions<T>
        where T : PersistenceDefinition
        where S : StorageType
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PersistenceExtensions" />.
        /// </summary>
        public PersistenceExtensions(SettingsHolder settings) : base(settings, typeof(S))
        {
        }
    }

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extension
    /// methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="InMemoryPersistence" />, etc.</typeparam>
    public class PersistenceExtensions<T> : PersistenceExtensions where T : PersistenceDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PersistenceExtensions(SettingsHolder settings) : base(typeof(T), settings, null)
        {
        }

        /// <summary>
        /// Constructor for a specific <see cref="StorageType" />.
        /// </summary>
        protected PersistenceExtensions(SettingsHolder settings, Type storageType) : base(typeof(T), settings, storageType)
        {
        }
    }

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extension
    /// methods.
    /// </summary>
    public class PersistenceExtensions : ExposeSettings
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PersistenceExtensions" />.
        /// </summary>
        public PersistenceExtensions(Type definitionType, SettingsHolder settings, Type storageType)
            : base(settings)
        {
            if (!Settings.TryGet("PersistenceDefinitions", out List<EnabledPersistence> definitions))
            {
                definitions = new List<EnabledPersistence>();
                Settings.Set("PersistenceDefinitions", definitions);
            }

            var enabledPersistence = new EnabledPersistence
            {
                DefinitionType = definitionType,
                SelectedStorages = new List<Type>()
            };

            if (storageType != null)
            {
                var definition = definitionType.Construct<PersistenceDefinition>();
                if (!definition.HasSupportFor(storageType))
                {
                    throw new Exception($"{definitionType.Name} does not support storage type {storageType.Name}. See http://docs.particular.net/nservicebus/persistence-in-nservicebus for supported variations.");
                }

                enabledPersistence.SelectedStorages.Add(storageType);
            }

            definitions.Add(enabledPersistence);
        }
    }
}