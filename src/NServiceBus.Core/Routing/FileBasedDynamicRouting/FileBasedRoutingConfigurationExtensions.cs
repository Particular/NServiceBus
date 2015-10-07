namespace NServiceBus
{
    using NServiceBus.Features;

    /// <summary>
    /// Extension methods to configure dynamic routing.
    /// </summary>
    public static class FileBasedRoutingConfigurationExtensions
    {
        /// <summary>
        /// Enables file-based route table source that is automatically refreshed whenever files get updated.
        /// </summary>
        public static FileRoutingTableSettings UseFileBasedEndpointInstanceLists(this RoutingSettings config)
        {
            config.Settings.EnableFeature(typeof(FileRoutingTableFeature));
            return new FileRoutingTableSettings(config.Settings);
        }
    }
}