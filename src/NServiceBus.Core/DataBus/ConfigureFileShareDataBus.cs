namespace NServiceBus
{
    using DataBus;

    /// <summary>
    /// Contains extension methods to <see cref="EndpointConfiguration" /> for the file share data bus.
    /// </summary>
    public static class ConfigureFileShareDataBus
    {
        /// <summary>
        /// Sets the location to which to write/read serialized properties for the databus.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="basePath">The location to which to write/read serialized properties for the databus.</param>
        /// <returns>The configuration.</returns>
        public static DataBusExtensions<FileShareDataBus> BasePath(this DataBusExtensions<FileShareDataBus> config, string basePath)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(basePath), basePath);
            config.Settings.Set("FileShareDataBusPath", basePath);

            return config;
        }
    }
}