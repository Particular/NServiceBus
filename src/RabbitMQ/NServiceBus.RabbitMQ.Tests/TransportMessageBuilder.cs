namespace NServiceBus.Transports.RabbitMQ.Tests
{
    using System;
    using NServiceBus;

    public class TransportMessageBuilder
    {
        readonly TransportMessage message = new TransportMessage{Recoverable = true};
    
        public TransportMessageBuilder WithBody(byte[] body)
        {
            message.Body = body;
            return this;
        }

        public TransportMessage Build()
        {
            return message;
        }

        public TransportMessageBuilder WithHeader(string key,string value)
        {
            message.Headers[key] = value;
            return this;
        }

        public TransportMessageBuilder TimeToBeReceived(TimeSpan timeToBeReceived)
        {

            message.TimeToBeReceived = timeToBeReceived;
            return this;
        }

        public TransportMessageBuilder ReplyToAddress(Address address)
        {
            message.ReplyToAddress = address;
            return this;
        }

        public TransportMessageBuilder CorrelationId(string correlationId)
        {
            message.CorrelationId = correlationId;
            return this;
        }
    }
}