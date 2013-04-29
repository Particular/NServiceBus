namespace NServiceBus.Transports.RabbitMQ.Routing
{
    using System;
    using global::RabbitMQ.Client;

    /// <summary>
    /// Route using a static routing convention for routing messages from publishers to subscribers using routing keys
    /// </summary>
    public class DirectRoutingTopology:IRoutingTopology
    {
        public Func<Address, Type, string> ExchangeNameConvention { get; set; }

        public Func<Type, string> RoutingKeyConvention { get; set; }

        public void SetupSubscription(IModel channel, Type type, string subscriberName)
        {
            CreateExchange(channel, ExchangeName());
            channel.QueueBind(subscriberName, ExchangeName(), GetRoutingKeyForBinding(type));
        }

        public void TeardownSubscription(IModel channel, Type type, string subscriberName)
        {
            channel.QueueUnbind(subscriberName, ExchangeName(), GetRoutingKeyForBinding(type), null);
        }

        public void Publish(IModel channel, Type type, TransportMessage message, IBasicProperties properties)
        {
            channel.BasicPublish(ExchangeName(), GetRoutingKeyForPublish(type), true, false, properties, message.Body);
        }

        public void Send(IModel channel, Address address, TransportMessage message, IBasicProperties properties)
        {
            channel.BasicPublish(string.Empty, address.Queue, true, false, properties, message.Body);
        }

        string ExchangeName()
        {
            return ExchangeNameConvention(null,null);
        }

        static void CreateExchange(IModel channel, string exchangeName)
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

        string GetRoutingKeyForPublish(Type eventType)
        {
            return RoutingKeyConvention(eventType);
        }

        string GetRoutingKeyForBinding(Type eventType)
        {
            if (eventType == typeof(IEvent) || eventType == typeof(object))
                return "#";


            return RoutingKeyConvention(eventType) + ".#";
        }

        const string AmqpTopicExchange = "amq.topic";


    }
}