namespace NServiceBus.RabbitMq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Queuing;

    public class RabbitMqMessagePublisher : IPublishMessages
    {
        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var eventType = eventTypes.First();//we route on the first event for now

            var routingKey = RabbitMqTopicBuilder.GetRoutingKeyForPublish(eventType);


            UnitOfWork.Add(channel =>
                {
                    var properties = RabbitMqTransportMessageExtensions.FillRabbitMqProperties(message,
                                                                                               channel.CreateBasicProperties());

                    channel.BasicPublish(EndpointQueueName + ".events", routingKey, true, false, properties, message.Body);
                });

            //we don't know if there was a subscriber so we just return true
            return true;
        }

        public string EndpointQueueName { get; set; }

        public RabbitMqUnitOfWork UnitOfWork { get; set; }
    }
}