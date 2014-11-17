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
        public static RoutingExtentions<FileBasedDynamicRouting> LookForFilesIn(this RoutingExtentions<FileBasedDynamicRouting> config, string path)
        {
            config.Settings.Set("FileBasedRouting.BasePath", path);
            return config;
        }

        /// <summary>
        /// Configures NServiceBus to use the given <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Dynamic routing definition.</typeparam>
        public static RoutingExtentions<T> UseDynamicRouting<T>(this BusConfiguration config) where T : DynamicRoutingDefinition
        {
            var type = typeof(RoutingExtentions<>).MakeGenericType(typeof(T));
            var extension = (RoutingExtentions<T>) Activator.CreateInstance(type, config.Settings);
            var definition = (DynamicRoutingDefinition) Activator.CreateInstance(typeof(T));

            config.Settings.Set("SelectedRouting", definition);

            config.EnableFeature<DynamicRouting>();

            return extension;
        }

        /// <summary>
        /// Sets the logical address translator to be used when dynamic routing is enabled.
        /// </summary>
        /// <param name="settings">The current settings instance.</param>
        /// <param name="translateToLogicalAddress">The callback to do the translation.</param>
        public static void SetDefaultTransportLogicalAddressTranslator(this SettingsHolder settings, Func<Address, string> translateToLogicalAddress)
        {
            if (!settings.HasExplicitValue("Routing.Translator"))
            {
                settings.Set("Routing.Translator", translateToLogicalAddress);
            }
        }
    }
}