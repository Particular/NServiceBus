namespace NServiceBus.Transport.RabbitMQ.Tests
{
    using System;
    using System.Collections.Generic;

    public class TransportMessageBuilder
    {
        readonly TransportMessage message = new TransportMessage();
    
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

        public TransportMessageBuilder Intent(MessageIntentEnum intent)
        {
            message.MessageIntent = intent;
            return this;
        }

        public TransportMessageBuilder CorrelationId(string correlationId)
        {
            message.CorrelationId = correlationId;
            return this;
        }
    }
}