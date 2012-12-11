namespace NServiceBus.ActiveMQ.Config
{
    using NServiceBus.Config;

    /// <summary>
    /// Default configuration for ActiveMQ
    /// </summary>
    public class ActiveMqTransportConfigurer : IConfigureTransport<ActiveMQ>
    {
        /// <summary>
        /// Configures ActiveMQ in the default mode
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            config.ActiveMqTransport();
        }
    }
}