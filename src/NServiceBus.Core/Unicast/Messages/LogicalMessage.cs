namespace NServiceBus.Unicast.Messages
{
    using System;

    class LogicalMessage
    {
        public LogicalMessage(MessageMetadata metadata, object message)
        {
            Instance = message;
            Metadata = metadata;
        }

        public void UpdateMessageInstance(object newMessage)
        {
            Instance = newMessage;
        }

        public Type MessageType
        {
            get
            {
                return Metadata.MessageType;
            }
        }

        public MessageMetadata Metadata { get; private set; }

        public object Instance { get; private set; }
    }
}