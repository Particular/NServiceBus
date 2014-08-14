namespace NServiceBus
{
    
    /// <summary>
    /// Contains extension methods to <see cref="ConfigurationBuilder"/> for the file share data bus
    /// </summary>
    public static partial class ConfigureFileShareDataBus
    {
        /// <summary>
        /// Use the file-based databus implementation with the default binary serializer.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="basePath">The location to which to write serialized properties for the databus.</param>
        /// <returns>The configuration.</returns>
        public static void FileShareDataBus(this ConfigurationBuilder config, string basePath)
        {
            config.settings.Set("FileShareDataBusPath", basePath);
        }

    }

}