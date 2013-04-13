namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using Routing;
    using Settings;

    public class RabbitMqConventions
    {
        /// <summary>
        /// Sets the convention for the name of the exchange(s) used for publish and subscribe
        /// </summary>
        /// <param name="exchangeNameConvention"></param>
        public RabbitMqConventions ExchangeNameForPubSub(Func<Address, Type, string> exchangeNameConvention)
        {
            SettingsHolder.Set("Conventions.RabbitMq.ExchangeNameForPubSub", exchangeNameConvention);
            return this;
        }


        /// <summary>
        /// Sets the convention that generates a routing key for a event type
        /// </summary>
        /// <param name="routingKeyConvention"></param>
        public RabbitMqConventions RoutingKeyForEvent(Func<Type, string> routingKeyConvention)
        {
            SettingsHolder.Set("Conventions.RabbitMq.RoutingKeyForEvent", routingKeyConvention);
            return this;
        }
    }

    class DefaultRabbitMqConventions : ISetDefaultSettings
    {
        public DefaultRabbitMqConventions()
        {
            Func<Address, Type, string> exchangeNameConvention = (address, eventType) => "amq.topic";
            Func<Type, string> routingKeyConvention = DefaultRoutingKeyConvention.GenerateRoutingKey;

            SettingsHolder.SetDefault("Conventions.RabbitMq.ExchangeNameForPubSub", exchangeNameConvention);
            SettingsHolder.SetDefault("Conventions.RabbitMq.RoutingKeyForEvent", routingKeyConvention);
            SettingsHolder.SetDefault("Conventions.RabbitMq.RoutingTopology", new DirectRoutingTopology{ExchangeNameConvention = exchangeNameConvention});
        }
    }
}