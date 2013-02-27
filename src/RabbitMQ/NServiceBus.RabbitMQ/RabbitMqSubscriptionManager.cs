﻿namespace NServiceBus.Transports.RabbitMQ
{
    using System;

    public class RabbitMqSubscriptionManager : IManageSubscriptions
    {
        public IManageRabbitMqConnections ConnectionManager { get; set; }

        public string EndpointQueueName { get; set; }

        public Func<Address,Type, string> ExchangeName { get; set; }

        public RabbitMqRoutingKeyBuilder RoutingKeyBuilder { get; set; }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = RoutingKeyBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
            {
                channel.QueueBind(EndpointQueueName, ExchangeName(publisherAddress,eventType), routingKey);
            }
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            var routingKey = RoutingKeyBuilder.GetRoutingKeyForBinding(eventType);

            using (var channel = ConnectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
            {
                channel.QueueUnbind(EndpointQueueName, ExchangeName(publisherAddress, eventType), routingKey, null);
            }
        }
    }
}