namespace NServiceBus
{
    using System;
    using System.Linq;
    using Config;
    using Unicast.Transport;

    /// <summary>
    /// 
    /// </summary>
    public static class TransactionalTransportConfig
    {
        /// <summary>
        /// Configures NServiceBus to use the given transport
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure UseTransport<T>(this Configure config)where T:ITransportDefinition
        {
            return UseTransport(config,typeof(T));
        }

        /// <summary>
        /// Configures NServiceBus to use the given transport
        /// </summary>
        /// <param name="config"></param>
        /// <param name="transportDefinitionType"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static Configure UseTransport(this Configure config,Type transportDefinitionType,string connectionString = null)
        {
            var transportConfigurerType =
              Configure.TypesToScan.SingleOrDefault(
                  t => typeof(IConfigureTransport<>).MakeGenericType(transportDefinitionType).IsAssignableFrom(t));

            if (transportConfigurerType == null)
                throw new InvalidOperationException("We couldn't find a IConfigureTransports implementation for your selected transport: " + transportDefinitionType.Name);

            var transportConfigurer = Activator.CreateInstance(transportConfigurerType) as IConfigureTransport;


            if(!string.IsNullOrEmpty(connectionString))
                TransportConnectionString.Override(()=>connectionString);

            transportConfigurer.Configure(config);

            return config;
        }
    }
}