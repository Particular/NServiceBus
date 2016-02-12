namespace NServiceBus
{
    using Features;

    /// <summary>
    /// Extension methods to configure dynamic routing.
    /// </summary>
    public static class FileBasedRoutingConfigurationExtensions
    {        
        /// <summary>
        /// Enables file-based route table source that is automatically refreshed whenever files get updated.
        /// </summary>
        public static FileRoutingTableSettings DistributeMessagesUsingFileBasedEndpointInstanceMapping(this RoutingMappingSettings config, string filePath)
        {
            Guard.AgainstNull(nameof(filePath), filePath);

            config.Settings.EnableFeature(typeof(FileRoutingTableFeature));
            config.Settings.Set(FileRoutingTableFeature.FilePathSettingsKey, filePath);
            return new FileRoutingTableSettings(config.Settings);
        }
    }
}