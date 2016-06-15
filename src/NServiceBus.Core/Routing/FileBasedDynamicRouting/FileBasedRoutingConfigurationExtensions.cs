namespace NServiceBus
{
    using Features;
    using Settings;

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
            return EnableFileBasedRouting(config.Settings, filePath);
        }

        internal static FileRoutingTableSettings EnableFileBasedRouting(SettingsHolder settings, string filePath)
        {
            Guard.AgainstNull(nameof(filePath), filePath);

            settings.EnableFeature(typeof(FileRoutingTableFeature));
            settings.Set(FileRoutingTableFeature.FilePathSettingsKey, filePath);
            return new FileRoutingTableSettings(settings);
        }
    }
}