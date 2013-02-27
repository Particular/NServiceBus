namespace NServiceBus.Transports.RabbitMQ
{
    using System;

    public class RabbitMqRoutingKeyBuilder
    {
        public Func<Type, String> GenerateRoutingKey { get; set; }

        public string GetRoutingKeyForPublish(Type eventType)
        {
            return GenerateRoutingKey(eventType);
        }

        public string GetRoutingKeyForBinding(Type eventType)
        {
            if (eventType == typeof(IEvent) || eventType == typeof(object))
                return "#";


            return GenerateRoutingKey(eventType) + ".#";
        }

       
    }
}