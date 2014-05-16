namespace NServiceBus
{
    using System;
    using System.Linq;
    using Transports;
    using Unicast.Transport;

    /// <summary>
    /// Extension methods to configure transport.
    /// </summary>
    public static class TransportReceiverConfig
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TransportDefinition"/> to be configured.</typeparam>
        /// <param name="config">The configuration object.</param>
        /// <param name="connectionStringName">The connection string name to use to retrieve the connection string from.</param> 
        /// <returns>The configuration object.</returns>
        public static Configure UseTransport<T>(this Configure config, string connectionStringName = null) where T : TransportDefinition
        {
            return UseTransport(config, typeof(T), connectionStringName);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TransportDefinition"/> to be configured.</typeparam>
        /// <param name="config">The configuration object.</param>
        /// <param name="definesConnectionString">Specifies a callback to call to retrieve the connection string to use</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseTransport<T>(this Configure config, Func<string> definesConnectionString) where T : TransportDefinition
        {
            return UseTransport(config, typeof(T), definesConnectionString);
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="transportDefinitionType">Type of <see cref="TransportDefinition"/> to be configured.</param>
        /// <param name="connectionStringName">The connection string name to use to retrieve the connection string from.</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseTransport(this Configure config, Type transportDefinitionType, string connectionStringName = null)
        {
            var transportConfigurer = CreateTransportConfigurer(transportDefinitionType);

            if (!string.IsNullOrEmpty(connectionStringName))
            {
                TransportConnectionString.DefaultConnectionStringName = connectionStringName;
            }

            transportConfigurer.Configure(config);

            return config;
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="transportDefinitionType">Type of <see cref="TransportDefinition"/> to be configured.</param>
        /// <param name="definesConnectionString">Specifies a callback to call to retrieve the connection string to use</param>
        /// <returns>The configuration object.</returns>
        public static Configure UseTransport(this Configure config, Type transportDefinitionType, Func<string> definesConnectionString)
        {
            var transportConfigurer = CreateTransportConfigurer(transportDefinitionType);

            TransportConnectionString.Override(definesConnectionString);
            
            transportConfigurer.Configure(config);

            return config;
        }



        private static IConfigureTransport CreateTransportConfigurer(Type transportDefinitionType)
        {
            var transportConfigurerType =
                Configure.TypesToScan.SingleOrDefault(
                    t => typeof (IConfigureTransport<>).MakeGenericType(transportDefinitionType).IsAssignableFrom(t));

            if (transportConfigurerType == null)
                throw new InvalidOperationException(
                    "We couldn't find a IConfigureTransport implementation for your selected transport: " +
                    transportDefinitionType.Name);

            var transportConfigurer = (IConfigureTransport) Activator.CreateInstance(transportConfigurerType);
            return transportConfigurer;
        }
    }
}