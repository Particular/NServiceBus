namespace NServiceBus.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using global::RabbitMQ.Client;

    public static class RabbitMqTransportMessageExtensions
    {
        public static IBasicProperties RabbitMqProperties(this TransportMessage message, IModel channel)
        {

            var properties = channel.CreateBasicProperties();

            properties.MessageId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(message.CorrelationId))
                properties.CorrelationId = message.CorrelationId;


            if (message.TimeToBeReceived < TimeSpan.MaxValue)
                properties.Expiration = message.TimeToBeReceived.TotalMilliseconds.ToString();

            

            properties.SetPersistent(message.Recoverable);

            if (message.Headers != null)
            {
                properties.Headers = message.Headers;

                if (message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
                {
                    properties.Type = message.Headers[Headers.EnclosedMessageTypes];
                }

                if (message.Headers.ContainsKey(Headers.ContentType))
                    properties.ContentType = message.Headers[Headers.ContentType];

                if (message.ReplyToAddress != null && message.ReplyToAddress != Address.Undefined)
                    properties.ReplyTo = message.ReplyToAddress.Queue;
                
            }
            else
            {
                properties.Headers = new Dictionary<string,string>();
            }

            properties.Headers["NServiceBus.MessageIntent"] = message.MessageIntent.ToString();

            return properties;
        }
    }
}