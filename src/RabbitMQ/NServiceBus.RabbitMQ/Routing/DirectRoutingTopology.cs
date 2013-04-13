namespace NServiceBus.Transports.RabbitMQ.Routing
{
    using System;
    using global::RabbitMQ.Client;

    /// <summary>
    /// Route using a static routing convention for routing messages from publishers to subscribers using routing keys
    /// </summary>
    public class DirectRoutingTopology:IRoutingTopology
    {
        public DirectRoutingTopology()
        {
            ExchangeNameConvention = (a, t) => AmqpTopicExchange;
        }

        public void SetupSubscription(IModel channel, Type type, string subscriberName)
        {
            CreateExchange(channel, ExchangeName());
            channel.QueueBind(subscriberName, ExchangeName(), RoutingKey(type));
        }

        public void TeardownSubscription(IModel channel, Type type, string subscriberName)
        {
            channel.QueueUnbind(subscriberName, ExchangeName(), RoutingKey(type), null);
        }

        public void Publish(IModel channel, Type type, TransportMessage message, IBasicProperties properties)
        {
            channel.BasicPublish(ExchangeName(), RoutingKey(type), true, false, properties, message.Body);
        }

        public Func<Address, Type, string> ExchangeNameConvention { get; set; }

        const string AmqpTopicExchange = "amq.topic";

        private readonly RabbitMqRoutingKeyBuilder routingKeyBuilder = new RabbitMqRoutingKeyBuilder
        {
            GenerateRoutingKey = DefaultRoutingKeyConvention.GenerateRoutingKey
        };

        private string ExchangeName()
        {
            return ExchangeNameConvention(null,null);
        }

        private static void CreateExchange(IModel channel, string exchangeName)
        {
            if (exchangeName == AmqpTopicExchange)
                return;
            try
            {
                channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, true);
            }
            catch (Exception)
            {

            }
        }

        private string RoutingKey(Type eventType)
        {
            return routingKeyBuilder.GetRoutingKeyForBinding(eventType);
        }
    }
}