namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Settings;

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
        /// </summary>
        /// <param name="settings"></param>
        public PersistenceExtentions(SettingsHolder settings) : base(settings, typeof(S))
        {
        }

        /// <summary>
        /// Defines the list of specific storage needs this persistence should provide
        /// </summary>
        /// <param name="specificStorages"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "5.2")]
        public new PersistenceExtentions<T> For(params Storage[] specificStorages)
        {
            throw new InvalidOperationException("Do not invoke .For() when StorageType is already specified.");
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
        /// <param name="settings"></param>
        /// <param name="storageType"></param>
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
            Replacement = "UsePersistence<T, S>()")]
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
            Replacement = "UsePersistence<T, S>()")]
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