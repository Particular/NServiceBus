namespace NServiceBus.Transports.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Routing;

    public class RabbitMqMessagePublisher : IPublishMessages
    {
        public IRoutingTopology RoutingTopology { get; set; }


        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var eventType = eventTypes.First();//we route on the first event for now



            UnitOfWork.Add(channel =>
                {
                    var properties = RabbitMqTransportMessageExtensions.FillRabbitMqProperties(message,
                                                                                               channel.CreateBasicProperties());

                    RoutingTopology.Publish(channel, eventType, message, properties);
                });

            //we don't know if there was a subscriber so we just return true
            return true;
        }

        public RabbitMqUnitOfWork UnitOfWork { get; set; }
    }
}