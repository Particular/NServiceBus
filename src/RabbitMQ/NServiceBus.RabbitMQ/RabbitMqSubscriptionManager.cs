namespace NServiceBus.RabbitMq
{
    using System;
    using Unicast;
    using Unicast.Subscriptions;
    using global::RabbitMQ.Client;

    public class RabbitMqSubscriptionManager : IManageSubscriptions
    {
        public IConnection Connection { get; set; }

        public string EndpointQueueName { get; set; }

        public void Subscribe(Type eventType, Address publisherAddress, Predicate<object> condition)
        {
            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = Connection.CreateModel())
            {
                channel.QueueBind(EndpointQueueName, publisherAddress.Queue + ".events", routingKey);
            }
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = Connection.CreateModel())
            {
                channel.QueueUnbind(EndpointQueueName, publisherAddress.Queue + ".events", routingKey,null);
            }
        }

        public event EventHandler<SubscriptionEventArgs> ClientSubscribed;
    }
}