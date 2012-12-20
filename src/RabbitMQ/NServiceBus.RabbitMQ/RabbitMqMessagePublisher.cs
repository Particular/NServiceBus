namespace NServiceBus.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqMessagePublisher : IPublishMessages
    {
        public void Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var routingKey = eventTypes.First().Name;

            using (var channel = Connection.CreateModel())
            {
                var properties = RabbitMqTransportMessageExtensions.FillRabbitMqProperties(message,
                                                                                           channel.CreateBasicProperties());

                channel.BasicPublish(EndpointQueueName + ".events", routingKey, true, false, properties, message.Body);
            }
        }

        public string EndpointQueueName { get; set; }
        public IConnection Connection { get; set; }
    }
}