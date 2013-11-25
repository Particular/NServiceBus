namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MessageInterfaces;

    class LogicalMessageFactory
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }
        
        public IMessageMapper MessageMapper { get; set; }

        public IEnumerable<LogicalMessage> Create<T>(T message)
        {
            return new[] { Create(message.GetType(), message) };
        }

        public LogicalMessage Create(Type messageType, object message)
        {
            var realMessageType = MessageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(MessageMetadataRegistry.GetMessageDefinition(realMessageType), message);
        }

        //in v5 we can skip this since we'll only support one message and the creation of messages happens under our control so we can capture 
        // the real message type without using the mapper
        [ObsoleteEx(RemoveInVersion = "5.0")]
        public IEnumerable<LogicalMessage> CreateMultiple(IEnumerable<object> messages)
        {
            if (messages == null)
            {
                return new List<LogicalMessage>();
            }


            return messages.Select(m =>
            {
                var messageType = MessageMapper.GetMappedTypeFor(m.GetType());

                return new LogicalMessage(MessageMetadataRegistry.GetMessageDefinition(messageType), m);
            }).ToList();
        }
    }
}