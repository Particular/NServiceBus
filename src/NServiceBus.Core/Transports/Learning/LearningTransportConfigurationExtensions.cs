namespace NServiceBus
{
    /// <summary>
    /// Configuration options for the learning transport.
    /// </summary>
    public static class LearningTransportConfigExtensions
    {
        /// <summary>
        /// Configures the location where message files are stored.
        /// </summary>
        /// <param name="config">Config object to extend.</param>
        /// <param name="path">The storage path.</param>
        public static void StorageDirectory(this TransportExtensions<LearningTransport> config, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);
            Guard.AgainstNull(nameof(config), config);
            PathChecker.ThrowForBadPath(path, "StorageDirectory");
            config.Settings.Set(LearningTransportInfrastructure.StorageLocationKey, path);
        }
    }
}