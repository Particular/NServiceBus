namespace NServiceBus
{
    /// <summary>
    /// Configuration options for the development transport.
    /// </summary>
    public static class DevelopmentTransportConfigExtensions
    {
        /// <summary>
        /// Configures the location where message files are stored.
        /// </summary>
        /// <param name="config">Config object to extend.</param>
        /// <param name="path">The storage path.</param>
        public static void StorageDirectory(this TransportExtensions<DevelopmentTransport> config, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);

            config.Settings.Set(DevelopmentTransportInfrastructure.StorageLocationKey, path);
        }
    }
}