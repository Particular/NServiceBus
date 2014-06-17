namespace NServiceBus.Persistence
{
    using System.Collections.Generic;

    /// <summary>
    /// Enables the given persistence using the default settings
    /// </summary>
    public interface IConfigurePersistence
    {
        /// <summary>
        /// Tells the storage to activate it self and provide the storages requested
        /// </summary>
        /// <param name="config">Access to the config object</param>
        /// <param name="storagesToEnable">The list of storages this persister is responsible for</param>
        void Enable(Configure config, List<Storage> storagesToEnable);
    }

    /// <summary>
    /// The generic counterpart to IConfigurePersistence
    /// </summary>
    public interface IConfigurePersistence<T> : IConfigurePersistence where T : PersistenceDefinition { }
}