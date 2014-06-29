namespace NServiceBus.Persistence
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides a hook for extention methods in order to provide custom configuration methods
    /// </summary>
    public class PersistenceConfiguration
    {
        /// <summary>
        /// Access to the current config instance
        /// </summary>
        public Configure Config { get; private set; }

        internal PersistenceConfiguration(Configure config)
        {
            Config = config;
        }

        /// <summary>
        /// Defines the list of specific storage needs this persistence should provide
        /// </summary>
        /// <param name="specificStorages">The list of storage needs</param>
        public void For(params Storage[] specificStorages)
        {
            SpecificStorages.AddRange(specificStorages.ToList());
        }

        internal List<Storage> SpecificStorages = new List<Storage>();
    }
}