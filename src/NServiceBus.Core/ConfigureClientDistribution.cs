namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Routing;

    /// <summary>
    /// Extension methods to configure Client Distribution.
    /// </summary>
    public static class ConfigureClientDistribution
    {
        /// <summary>
        /// Folder path where to look for files when using <see cref="FileBasedDynamicRouting"/>.
        /// </summary>
        /// <param name="config">The current definition instance.</param>
        /// <param name="path">The folder path. This can be a UNC path.</param>
        public static RoutingExtentions<FileBasedDynamicRouting> LookForFilesIn(this RoutingExtentions<FileBasedDynamicRouting> config, string path)
        {
            config.Settings.Set("FileBasedRouting.BasePath", path);
            return config;
        }

        /// <summary>
        /// Configures NServiceBus to use the given Client Distribution definition.
        /// </summary>
        /// <typeparam name="T">Client Distribution definition.</typeparam>
        public static RoutingExtentions<T> UseClientDistribution<T>(this BusConfiguration config) where T : DynamicRoutingDefinition
        {
            var type = typeof(RoutingExtentions<>).MakeGenericType(typeof(T));
            var extension = (RoutingExtentions<T>)Activator.CreateInstance(type, config.Settings);
            var definition = (DynamicRoutingDefinition)Activator.CreateInstance(typeof(T));

            config.Settings.Set("SelectedRouting", definition);

            config.EnableFeature<DynamicRouting>();

            return extension;
        }
    }
}