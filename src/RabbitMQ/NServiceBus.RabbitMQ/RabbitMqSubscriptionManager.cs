namespace NServiceBus.RabbitMq
{
    using System;
    using Unicast.Subscriptions;


    public class RabbitMqSubscriptionManager : IManageSubscriptions
    {
        public IManageRabbitMqConnections ConnectionManager { get; set; }

        public string EndpointQueueName { get; set; }

        public Func<Address, string> ExchangeName { get; set; }
        
        public void Subscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration,"subscriptions").CreateModel())
            {
                channel.QueueBind(EndpointQueueName, ExchangeName(publisherAddress), routingKey);
            }
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration, "subscriptions").CreateModel())
            {
                channel.QueueUnbind(EndpointQueueName, ExchangeName(publisherAddress), routingKey, null);
            }
        }
    }
}