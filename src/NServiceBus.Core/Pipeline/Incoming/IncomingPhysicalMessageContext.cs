namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    class IncomingPhysicalMessageContext : IncomingContext, IIncomingPhysicalMessageContext
    {
        public IncomingPhysicalMessageContext(string messageId, Dictionary<string, string> headers, byte[] body, IBehaviorContext parentContext)
            : base(messageId, /*message.GetReplyToAddress()*/ null, headers, parentContext)
        {
            Body = body;
            Headers = headers;
        }

        public byte[] Body { get; }
        public Dictionary<string, string> Headers { get; }

        public void RevertToOriginalBodyIfNeeded()
        {
        }

        public void UpdateMessage(byte[] body)
        {
           // Message.Body = body;
        }
    }
}