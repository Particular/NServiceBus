namespace NServiceBus.Outbox
{
    using System.Collections.Generic;

    public class TransportOperation
    {
        public string MessageId { get; private set; }
        public Dictionary<string, string> Options { get; private set; }
        public byte[] Body { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }

        public TransportOperation(string messageId, Dictionary<string, string> options, byte[] body, Dictionary<string, string> headers)
        {
            MessageId = messageId;
            Options = options;
            Body = body;
            Headers = headers;
        }
    }
}