namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Settings;
    using Utils.Reflection;

    /// <summary> 
    /// This class provides implementers of persisters with an extension mechanism for custom settings for specific storage type via extention methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="NServiceBus.InMemory" />, <see cref="MsmqTransport" />, etc</typeparam>
    /// <typeparam name="S">The <see cref="StorageType"/>storage type</typeparam>
    public class PersistenceExtentions<T, S> : PersistenceExtentions<T>
        where T : PersistenceDefinition
        where S : StorageType
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public PersistenceExtentions(SettingsHolder settings) : base(settings, typeof(S))
        {
        }
    }

    /// <summary>
    ///     This class provides implementers of persisters with an extension mechanism for custom settings via extention
    ///     methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="NServiceBus.InMemory" />, <see cref="MsmqTransport" />, etc</typeparam>
    public class PersistenceExtentions<T> : PersistenceExtentions where T : PersistenceDefinition
    {
        /// <summary>
        ///     Default constructor.
        /// </summary>
        public PersistenceExtentions(SettingsHolder settings) : base(typeof(T), settings, null)
        {
        }

        /// <summary>
        /// Constructor for a specific <see cref="StorageType"/>
        /// </summary>
        protected PersistenceExtentions(SettingsHolder settings, Type storageType) : base(typeof(T), settings, storageType)
        {
        }

        /// <summary>
        ///     Defines the list of specific storage needs this persistence should provide
        /// </summary>
        /// <param name="specificStorages">The list of storage needs</param>
         [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "UsePersistence<T, S>()")]
        public new PersistenceExtentions<T> For(params Storage[] specificStorages)
        {
            base.For(specificStorages);
            return this;
        }
    }

    /// <summary>
    ///     This class provides implementers of persisters with an extension mechanism for custom settings via extention
    ///     methods.
    /// </summary>
    public class PersistenceExtentions : ExposeSettings
    {
        /// <summary>
        ///     Default constructor.
        /// </summary>
        public PersistenceExtentions(Type definitionType, SettingsHolder settings, Type storageType)
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
                SelectedStorages = new List<Type>(),
            };

            
            if (storageType != null)
            {
                var definition = definitionType.Construct<PersistenceDefinition>();
                if (!definition.HasSupportFor(storageType))
                {
                    throw new Exception(string.Format("{0} does not support storage type {1}. See http://docs.particular.net/nservicebus/persistence-in-nservicebus for supported variations.", definitionType.Name, storageType.Name));
                }

                enabledPersistence.SelectedStorages.Add(storageType);
            }

            definitions.Add(enabledPersistence);
        }


        /// <summary>
        ///     Defines the list of specific storage needs this persistence should provide
        /// </summary>
        /// <param name="specificStorages">The list of storage needs</param>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "UsePersistence<T, S>()")]
        public PersistenceExtentions For(params Storage[] specificStorages)
        {
            if (specificStorages == null || specificStorages.Length == 0)
            {
                throw new ArgumentException("Please make sure you specify at least one Storage.");
            }

            var list = specificStorages.Select(StorageType.FromEnum).ToArray();
            enabledPersistence.SelectedStorages.AddRange(list);

            return this;
        }

        EnabledPersistence enabledPersistence;
    }
}