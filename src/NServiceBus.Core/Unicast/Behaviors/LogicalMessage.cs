namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using MessageInterfaces;

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


        public static IEnumerable<LogicalMessage> Create<T>(T message)
        {
            return new[]{new LogicalMessage(typeof(T), message)};
        }


        //in v5 we can skip this since we'll only support one message and the creation of messages happens under our control so we can capture 
        // the real message type without using the mapper
        [ObsoleteEx(RemoveInVersion = "5.0")]
        public static IEnumerable<LogicalMessage> Create(IEnumerable<object> messages,IMessageMapper mapper)
        {
            if (messages == null)
            {
                return new List<LogicalMessage>();
            }

            
            return messages.Select(m => new LogicalMessage(mapper.GetMappedTypeFor(m.GetType()), m)).ToList();
        }
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