namespace NServiceBus.Transport.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqMessagePublisher : IPublishMessages
    {
        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var routingKey = eventTypes.First().Name;

            using (var channel = Connection.CreateModel())
            {
                var properties = RabbitMqTransportMessageExtensions.FillRabbitMqProperties(message,
                                                                                           channel.CreateBasicProperties());

                channel.BasicPublish(EndpointQueueName + ".events", routingKey, true, false, properties, message.Body);
            }

            //we don't know if there was a subscriber so we just return true
            return true;
        }

        public string EndpointQueueName { get; set; }
        public IConnection Connection { get; set; }
    }
}