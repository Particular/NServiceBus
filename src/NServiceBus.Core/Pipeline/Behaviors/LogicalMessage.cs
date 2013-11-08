namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    class LogicalMessage
    {
        public LogicalMessage(Type messageType, object message)
        {
            Instance = message;
            MessageType = messageType;
        }

        public void UpdateMessageInstance(object newMessage)
        {
            Instance = newMessage;
        }

        public Type MessageType { get; private set; }
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