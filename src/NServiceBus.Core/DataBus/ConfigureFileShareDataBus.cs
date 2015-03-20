namespace NServiceBus
{
    using NServiceBus.DataBus;

    /// <summary>
    /// Contains extension methods to <see cref="BusConfiguration"/> for the file share data bus
    /// </summary>
    public static class ConfigureFileShareDataBus
    {
        /// <summary>
        /// The location to which to write/read serialized properties for the databus.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="basePath">The location to which to write/read serialized properties for the databus.</param>
        /// <returns>The configuration.</returns>
        public static DataBusExtentions<FileShareDataBus> BasePath(this DataBusExtentions<FileShareDataBus> config, string basePath)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNullAndEmpty(basePath, "basePath");
            config.Settings.Set("FileShareDataBusPath", basePath);

            return config;
        }
    }

}
