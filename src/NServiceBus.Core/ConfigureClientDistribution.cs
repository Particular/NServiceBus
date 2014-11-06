namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Unicast.Routing;

    /// <summary>
    /// Extension methods to configure Client Distribution.
    /// </summary>
    public static class ConfigureClientDistribution
    {
        /// <summary>
        /// Folder path where to look for files when using <see cref="FileBasedRoutingDistributor"/>.
        /// </summary>
        /// <param name="config">The current definition instance.</param>
        /// <param name="path">The folder path. This can be a UNC path.</param>
        public static RoutingExtentions<FileBasedRoutingDistributor> LookForFilesIn(this RoutingExtentions<FileBasedRoutingDistributor> config, string path)
        {
            config.Settings.Set("FileBasedRouting.BasePath", path);
            return config;
        }

        /// <summary>
        /// Configures NServiceBus to use the given Client Distribution definition.
        /// </summary>
        /// <typeparam name="T">Client Distribution definition.</typeparam>
        public static RoutingExtentions<T> UseClientDistribution<T>(this BusConfiguration config) where T : RoutingDistributorDefinition
        {
            var type = typeof(RoutingExtentions<>).MakeGenericType(typeof(T));
            var extension = (RoutingExtentions<T>)Activator.CreateInstance(type, config.Settings);
            var definition = (RoutingDistributorDefinition)Activator.CreateInstance(typeof(T));

            config.Settings.Set("SelectedRouting", definition);

            config.EnableFeature<RoutingDistributor>();

            return extension;
        }
    }
}