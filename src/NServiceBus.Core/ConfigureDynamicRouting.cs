namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NServiceBus.Settings;

    /// <summary>
    /// Extension methods to configure dynamic routing.
    /// </summary>
    public static class ConfigureDynamicRouting
    {
        /// <summary>
        /// Folder path where to look for files when using <see cref="FileBasedDynamicRouting"/>.
        /// </summary>
        /// <param name="config">The current definition instance.</param>
        /// <param name="path">The folder path. This can be a UNC path.</param>
        public static RoutingExtensions<FileBasedDynamicRouting> LookForFilesIn(this RoutingExtensions<FileBasedDynamicRouting> config, string path)
        {
            config.Settings.Set("FileBasedRouting.BasePath", path);
            return config;
        }

        /// <summary>
        /// Configures NServiceBus to use the given <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Dynamic routing definition.</typeparam>
        public static RoutingExtensions<T> UseDynamicRouting<T>(this BusConfiguration config) where T : DynamicRoutingDefinition
        {
            var type = typeof(RoutingExtensions<>).MakeGenericType(typeof(T));
            var extension = (RoutingExtensions<T>) Activator.CreateInstance(type, config.Settings);
            var definition = (DynamicRoutingDefinition) Activator.CreateInstance(typeof(T));

            config.Settings.Set("SelectedRouting", definition);

            config.EnableFeature<DynamicRouting>();

            return extension;
        }
    }
}