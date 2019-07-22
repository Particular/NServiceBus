namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions<T> UseTransport<T>(this EndpointConfiguration endpointConfiguration) where T : TransportDefinition, new()
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            var type = typeof(TransportExtensions<>).MakeGenericType(typeof(T));
            var extension = (TransportExtensions<T>)Activator.CreateInstance(type, endpointConfiguration.Settings);

            var transportDefinition = new T();
            ConfigureTransport(endpointConfiguration, transportDefinition);
            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions UseTransport(this EndpointConfiguration endpointConfiguration, Type transportDefinitionType)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(transportDefinitionType), transportDefinitionType);
            Guard.TypeHasDefaultConstructor(transportDefinitionType, nameof(transportDefinitionType));

            var transportDefinition = transportDefinitionType.Construct<TransportDefinition>();
            ConfigureTransport(endpointConfiguration, transportDefinition);
            return new TransportExtensions(endpointConfiguration.Settings);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static TransportExtensions<T> UseTransport<T>(this EndpointConfiguration endpointConfiguration, T transportDefinition) where T : TransportDefinition
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            ConfigureTransport(endpointConfiguration, transportDefinition);
            return new TransportExtensions<T>(endpointConfiguration.Settings);
        }

        static void ConfigureTransport(EndpointConfiguration endpointConfiguration, TransportDefinition transportDefinition)
        {
            endpointConfiguration.Settings.Set(transportDefinition);
        }
    }
}