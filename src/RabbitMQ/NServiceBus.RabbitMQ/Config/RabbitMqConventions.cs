namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
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
    }

    class DefaultRabbitMqConventions : ISetDefaultSettings
    {
        public DefaultRabbitMqConventions()
        {
            Func<Address, Type, string> exchangeNameConvention = (address, eventType) => "amq.topic";
            SettingsHolder.SetDefault("Conventions.RabbitMq.ExchangeNameForPubSub", exchangeNameConvention);
        }
    }
}