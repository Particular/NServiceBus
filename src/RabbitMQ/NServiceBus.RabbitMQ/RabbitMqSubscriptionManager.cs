namespace NServiceBus.RabbitMQ
{
    using System;
    using Unicast.Subscriptions;
    using global::RabbitMQ.Client;

    public class RabbitMqSubscriptionManager : IManageSubscriptions
    {
        public IConnection Connection { get; set; }

        public string EndpointQueueName { get; set; }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = eventType.Name;

            using (var channel = Connection.CreateModel())
            {
                channel.QueueBind(EndpointQueueName, publisherAddress.Queue + ".events", routingKey);
            }
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = eventType.Name;

            using (var channel = Connection.CreateModel())
            {
                channel.QueueUnbind(EndpointQueueName, publisherAddress.Queue + ".events", routingKey,null);
            }
        }
    }
}