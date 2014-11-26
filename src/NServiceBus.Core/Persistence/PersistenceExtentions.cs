namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Settings;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="S"></typeparam>
    public class PersistenceExtentions<T, S> : PersistenceExtentions<T> where T : PersistenceDefinition
        where S : StorageOption
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public PersistenceExtentions(SettingsHolder settings) : base(settings)
        {
        }
    }

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extention methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="NServiceBus.InMemory"/>, <see cref="MsmqTransport"/>, etc</typeparam>
    public class PersistenceExtentions<T> : PersistenceExtentions where T : PersistenceDefinition
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PersistenceExtentions(SettingsHolder settings)
            : base(typeof(T), settings)
        {
        }

        /// <summary>
        /// Defines the list of specific storage needs this persistence should provide
        /// </summary>
        /// <param name="specificStorages">The list of storage needs</param>
        public new PersistenceExtentions<T> For(params Storage[] specificStorages)
        {
            base.For(specificStorages);
            return this;
        }
    }

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extention methods.
    /// </summary>
    public class PersistenceExtentions : ExposeSettings
    {
        readonly EnabledPersistence enabledPersistence;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PersistenceExtentions(Type definitionType, SettingsHolder settings)
            : base(settings)
        {
            List<EnabledPersistence> definitions;
            if (!Settings.TryGet("PersistenceDefinitions", out definitions))
            {
                definitions = new List<EnabledPersistence>();
                Settings.Set("PersistenceDefinitions", definitions);
            }

            enabledPersistence = new EnabledPersistence
            {
                DefinitionType = definitionType,
                SelectedStorages = new List<Storage>(),
            };
            definitions.Add(enabledPersistence);
        }

        /// <summary>
        /// Defines the list of specific storage needs this persistence should provide
        /// </summary>
        /// <param name="specificStorages">The list of storage needs</param>
        public PersistenceExtentions For(params Storage[] specificStorages)
        {
            if (specificStorages == null || specificStorages.Length == 0)
            {
                throw new ArgumentException("Please make sure you specify at least one Storage.");
            }

            enabledPersistence.SelectedStorages.AddRange(specificStorages);

            return this;
        }
    }
}