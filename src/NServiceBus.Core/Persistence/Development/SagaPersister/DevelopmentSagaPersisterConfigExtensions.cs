namespace NServiceBus
{
    using Features;
    using Persistence;

    /// <summary>
    /// Configuration options for the development saga persister.
    /// </summary>
    public static class DevelopmentSagaPersisterConfigExtensions
    {
        /// <summary>
        /// Configures the location where sagas are stored.
        /// </summary>
        /// <param name="config">Config object to extend.</param>
        /// <param name="path">The storage path.</param>
        public static void SagaStorageDirectory(this PersistenceExtensions<DevelopmentPersistence, StorageType.Sagas> config, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);

            config.Settings.Set(DevelopmentSagaPersistence.StorageLocationKey, path);
        }
    }
}