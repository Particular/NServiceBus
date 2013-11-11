namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

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

    class LogicalMessages : IEnumerable<LogicalMessage>
    {
        public void Add(LogicalMessage message)
        {
            messages.Add(message);
        }

        public IEnumerator<LogicalMessage> GetEnumerator()
        {
            return messages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        List<LogicalMessage> messages = new List<LogicalMessage>();
    }
}