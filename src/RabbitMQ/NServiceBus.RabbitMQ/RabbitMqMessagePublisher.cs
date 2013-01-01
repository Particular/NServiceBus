namespace NServiceBus.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Unicast.Queuing;
    using global::RabbitMQ.Client;

    public class RabbitMqMessagePublisher : IPublishMessages
    {
        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var eventType = eventTypes.First();//we route on the first event for now
            
            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForPublish(eventType);

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