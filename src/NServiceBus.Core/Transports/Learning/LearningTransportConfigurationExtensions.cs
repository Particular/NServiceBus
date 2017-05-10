namespace NServiceBus
{
    /// <summary>
    /// Configuration options for the learning transport.
    /// </summary>
    public static class LearningTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures the location where message files are stored.
        /// </summary>
        /// <param name="transportExtensions">The transport extensions to extend.</param>
        /// <param name="path">The storage path.</param>
        public static void StorageDirectory(this TransportExtensions<LearningTransport> transportExtensions, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);
            PathChecker.ThrowForBadPath(path, "StorageDirectory");

            transportExtensions.Settings.Set(LearningTransportInfrastructure.StorageLocationKey, path);
        }

        /// <summary>
        /// Allows messages of any size to be sent.
        /// </summary>
        /// <param name="transportExtensions">The transport extensions to extend.</param>
        public static void NoPayloadSizeRestriction(this TransportExtensions<LearningTransport> transportExtensions)
        {
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);

            transportExtensions.Settings.Set(LearningTransportInfrastructure.NoPayloadSizeRestrictionKey, true);
        }
    }
}
