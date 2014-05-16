namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;


    public class LogicalMessage
    {
        readonly LogicalMessageFactory factory;

        internal LogicalMessage(Dictionary<string, string> headers, LogicalMessageFactory factory)
        {
            this.factory = factory;
            Metadata = new MessageMetadata();
            Headers = headers;
        }

        internal LogicalMessage(MessageMetadata metadata, object message, Dictionary<string, string> headers, LogicalMessageFactory factory)
        {
            this.factory = factory;
            Instance = message;
            Metadata = metadata;
            Headers = headers;
        }

        public void UpdateMessageInstance(object newMessage)
        {
            var sameInstance = ReferenceEquals(Instance, newMessage);
            
            Instance = newMessage;

            if (sameInstance)
            {
                return;
            }

            var newLogicalMessage = factory.Create(newMessage);

            Metadata = newLogicalMessage.Metadata;
        }

        public Type MessageType
        {
            get
            {
                return Metadata.MessageType;
            }
        }

        public Dictionary<string, string> Headers { get; private set; }

        public MessageMetadata Metadata { get; private set; }

        public object Instance { get; private set; }
    }
}