namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// This class provides implementers of persisters with an extension mechanism for custom settings via extention methods.
    /// </summary>
    /// <typeparam name="T">The persister definition eg <see cref="InMemory"/>, <see cref="Msmq"/>, etc</typeparam>
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
        readonly Type definitionType;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PersistenceExtentions(Type definitionType, SettingsHolder settings)
            : base(settings)
        {
            this.definitionType = definitionType;
        }

        /// <summary>
        /// Defines the list of specific storage needs this persistence should provide
        /// </summary>
        /// <param name="specificStorages">The list of storage needs</param>
        public PersistenceExtentions For(params Storage[] specificStorages)
        {
            List<EnabledPersistence> definitions;
            if (!Settings.TryGet("PersistenceDefinitions", out definitions))
            {
                definitions = new List<EnabledPersistence>();
                Settings.Set("PersistenceDefinitions", definitions);
            }

            definitions.Add(new EnabledPersistence
            {
                DefinitionType = definitionType,
                SelectedStorages = specificStorages
            });

            return this;
        }

    }
}