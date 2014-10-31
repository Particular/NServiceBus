namespace NServiceBus
{
    using NServiceBus.DataBus;

    /// <summary>
    /// Contains extension methods to <see cref="BusConfiguration"/> for the file share data bus
    /// </summary>
    public static partial class ConfigureFileShareDataBus
    {
        /// <summary>
        /// Use the file-based databus implementation with the default binary serializer.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="basePath">The location to which to write/read serialized properties for the databus.</param>
        /// <returns>The configuration.</returns>
       [ObsoleteEx(
            Message = "Use `configuration.UseDataBus<FileShareDataBus>().BasePath(basePath)`, where `configuration` is an instance of `BusConfiguration`. If self-hosting the instance can be obtained from `new BusConfiguration()`. if using the NServiceBus Host the instance of `BusConfiguration` will be passed in via the `INeedInitialization` or `IConfigureThisEndpoint` interfaces.",
            RemoveInVersion = "6.0",
            TreatAsErrorFromVersion = "5.5")]
        public static void FileShareDataBus(this BusConfiguration config, string basePath)
        {
            config.Settings.Set("FileShareDataBusPath", basePath);
        }

        /// <summary>
        /// The location to which to write/read serialized properties for the databus.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="basePath">The location to which to write/read serialized properties for the databus.</param>
        /// <returns>The configuration.</returns>
        public static DataBusExtentions<FileShareDataBus> BasePath(this DataBusExtentions<FileShareDataBus> config, string basePath)
        {
            config.Settings.Set("FileShareDataBusPath", basePath);

            return config;
        }
    }

}
