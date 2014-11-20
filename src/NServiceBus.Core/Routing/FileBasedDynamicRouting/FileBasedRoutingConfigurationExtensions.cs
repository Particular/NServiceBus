namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Routing;

    /// <summary>
    /// Extension methods to configure dynamic routing.
    /// </summary>
    public static class FileBasedRoutingConfigurationExtensions
    {
        /// <summary>
        /// Folder path where to look for files when using <see cref="FileBasedRoundRobinDistribution"/>.
        /// </summary>
        /// <param name="config">The current definition instance.</param>
        /// <param name="path">The folder path. This can be a UNC path.</param>
        public static RoutingExtensions<FileBasedRoundRobinDistribution> LookForFilesIn(this RoutingExtensions<FileBasedRoundRobinDistribution> config, string path)
        {
            config.Settings.Set("FileBasedRouting.BasePath", path);
            return config;
        }
    }
}