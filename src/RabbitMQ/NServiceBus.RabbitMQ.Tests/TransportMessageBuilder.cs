namespace NServiceBus.RabbitMQ.Tests
{
    using System;
    using System.Collections.Generic;

    public class TransportMessageBuilder
    {
        readonly TransportMessage message = new TransportMessage();
        readonly Dictionary<string, string> headers = new Dictionary<string, string>();

        public TransportMessageBuilder WithBody(byte[] body)
        {
            message.Body = body;
            return this;
        }

        public TransportMessage Build()
        {
            message.Headers = headers;

            return message;
        }

        public TransportMessageBuilder WithHeader(string key,string value)
        {
            headers.Add(key,value);
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
    }
}