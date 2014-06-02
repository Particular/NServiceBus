namespace NServiceBus
{
    using System;
    using System.Linq;
    using Transports;
    using Unicast.Transport;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class UseTransportExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static Configure UseTransport<T>(this Configure config, Action<TransportConfiguration> customizations = null) where T : TransportDefinition
        {
            return UseTransport(config, typeof(T), customizations);
        }

        
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        public static Configure UseTransport(this Configure config, Type transportDefinitionType, Action<TransportConfiguration> customizations = null)
        {
            config.Settings.SetDefault<TransportConnectionString>(TransportConnectionString.Default);

            var transportConfigurerType =
               config.TypesToScan.SingleOrDefault(
                   t => typeof(IConfigureTransport<>).MakeGenericType(transportDefinitionType).IsAssignableFrom(t));

            if (transportConfigurerType == null)
                throw new InvalidOperationException(
                    "We couldn't find a IConfigureTransport implementation for your selected transport: " +
                    transportDefinitionType.Name);

         if (customizations != null)
            {
                customizations(new TransportConfiguration(config));
            }

            ((IConfigureTransport)Activator.CreateInstance(transportConfigurerType)).Configure(config);


            return config;
        }
    }
}