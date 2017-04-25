namespace NServiceBus
{
    using Features;

    /// <summary>
    /// Configuration options for the learning saga persister.
    /// </summary>
    public static class LearningSagaPersisterConfigExtensions
    {
        /// <summary>
        /// Configures the location where sagas are stored.
        /// </summary>
        /// <param name="config">Config object to extend.</param>
        /// <param name="path">The storage path.</param>
        public static void SagaStorageDirectory(this PersistenceExtensions<LearningPersistence> config, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);

            config.Settings.Set(LearningSagaPersistence.StorageLocationKey, path);
        }
    }
}