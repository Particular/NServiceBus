namespace NServiceBus
{
    using System;
    using System.Reflection;
    using Transport;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions<T> UseTransport<T>(this RawEndpointConfiguration endpointConfiguration) where T : TransportDefinition, new()
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var transportDefinition = new T();
            var extension = (TransportExtensions<T>) Activator.CreateInstance(type, endpointConfiguration.Settings, transportDefinition);

            ConfigureTransport(endpointConfiguration, transportDefinition);
            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions UseTransport(this RawEndpointConfiguration endpointConfiguration, Type transportDefinitionType)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(transportDefinitionType), transportDefinitionType);
            Guard.TypeHasDefaultConstructor(transportDefinitionType, nameof(transportDefinitionType));

            var transportDefinition = transportDefinitionType.Construct<TransportDefinition>();
            ConfigureTransport(endpointConfiguration, transportDefinition);
            return new TransportExtensions(endpointConfiguration.Settings, transportDefinition);
        }

        static void ConfigureTransport(RawEndpointConfiguration endpointConfiguration, TransportDefinition transportDefinition)
        {
            endpointConfiguration.Settings.Set<TransportDefinition>(transportDefinition);
        }

        static T Construct<T>(this Type type)
        {
            var defaultConstructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[]
            {
            }, null);
            if (defaultConstructor != null)
            {
                return (T)defaultConstructor.Invoke(null);
            }

            return (T)Activator.CreateInstance(type);
        }
    }
}