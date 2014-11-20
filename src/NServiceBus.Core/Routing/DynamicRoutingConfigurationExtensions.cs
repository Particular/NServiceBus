namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Routing;

    /// <summary>
    /// Extension methods to configure dynamic routing.
    /// </summary>
    public static class DynamicRoutingConfigurationExtensions
    {
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