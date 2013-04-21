namespace NServiceBus
{
    using System;
    using Settings;
    using Transports.RabbitMQ.Config;

    /// <summary>
    /// Adds access to the RabbitMQ transport config to the global Transports object
    /// </summary>
    public static class RabbitMqSettingsExtensions
    {
        /// <summary>
        /// RabbitMq settings.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="action">The actual setting</param>
        /// <returns></returns>
        public static TransportSettings RabbitMq(this TransportSettings configuration, Action<RabbitMqSettings> action)
        {
            action(new RabbitMqSettings());
            return configuration;
        }
    }
}