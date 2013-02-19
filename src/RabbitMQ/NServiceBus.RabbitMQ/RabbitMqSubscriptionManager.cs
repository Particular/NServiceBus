namespace NServiceBus.RabbitMq
{
    using System;
    using Unicast.Subscriptions;
    using global::RabbitMQ.Client;

    public class RabbitMqSubscriptionManager : IManageSubscriptions
    {
        public IConnection Connection { get; set; }

        public string EndpointQueueName { get; set; }

        public Func<Address, string> ExchangeName { get; set; }
        
        public void Subscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = Connection.CreateModel())
            {
                channel.QueueBind(EndpointQueueName, ExchangeName(publisherAddress), routingKey);
            }
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = Connection.CreateModel())
            {
                channel.QueueUnbind(EndpointQueueName, ExchangeName(publisherAddress), routingKey, null);
            }
        }
    }
}